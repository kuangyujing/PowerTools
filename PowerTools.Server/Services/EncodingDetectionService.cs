using System.Text;
using Ude;

namespace PowerTools.Server.Services;

/// <summary>
/// Service for detecting and converting file encodings
/// </summary>
public class EncodingDetectionService
{
    private readonly ILogger<EncodingDetectionService> _logger;

    public EncodingDetectionService(ILogger<EncodingDetectionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Convert file content from one encoding to another
    /// </summary>
    public ConversionResult Convert(byte[] fileBytes, string? inputEncoding, string outputEncoding)
    {
        // Validate input
        if (fileBytes == null || fileBytes.Length == 0)
            throw new ArgumentException("File content cannot be empty", nameof(fileBytes));

        Encoding sourceEncoding;
        double? confidence = null;

        // Determine source encoding
        if (!string.IsNullOrWhiteSpace(inputEncoding))
        {
            // Use specified encoding
            try
            {
                sourceEncoding = Encoding.GetEncoding(inputEncoding);
                _logger.LogInformation("Using specified input encoding: {Encoding}", inputEncoding);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Unsupported input encoding: {inputEncoding}", nameof(inputEncoding));
            }
        }
        else
        {
            // Auto-detect encoding
            var detectionResult = DetectEncoding(fileBytes);
            sourceEncoding = detectionResult.Encoding;
            confidence = detectionResult.Confidence;
            _logger.LogInformation("Auto-detected encoding: {Encoding} (confidence: {Confidence:P0})",
                sourceEncoding.WebName, confidence);
        }

        // Get target encoding
        Encoding targetEncoding;
        try
        {
            targetEncoding = Encoding.GetEncoding(outputEncoding);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException($"Unsupported output encoding: {outputEncoding}", nameof(outputEncoding));
        }

        // Decode and re-encode
        string text = sourceEncoding.GetString(fileBytes);
        byte[] convertedBytes = targetEncoding.GetBytes(text);

        return new ConversionResult
        {
            ConvertedBytes = convertedBytes,
            DetectedEncoding = sourceEncoding.WebName,
            OutputEncoding = targetEncoding.WebName,
            DetectionConfidence = confidence
        };
    }

    /// <summary>
    /// Detect file encoding using multiple strategies
    /// </summary>
    private DetectionResult DetectEncoding(byte[] fileBytes)
    {
        // Step 1: Check for BOM
        var bomEncoding = DetectEncodingFromBOM(fileBytes);
        if (bomEncoding != null)
        {
            _logger.LogInformation("Detected encoding from BOM: {Encoding}", bomEncoding.WebName);
            return new DetectionResult { Encoding = bomEncoding, Confidence = 1.0 };
        }

        // Step 2: Use Ude.NetStandard for statistical detection
        var detector = new CharsetDetector();
        detector.Feed(fileBytes, 0, fileBytes.Length);
        detector.DataEnd();

        if (detector.Charset != null && detector.Confidence > 0.8)
        {
            try
            {
                var encoding = Encoding.GetEncoding(detector.Charset);
                _logger.LogInformation("Ude detected: {Charset} (confidence: {Confidence:P0})",
                    detector.Charset, detector.Confidence);
                return new DetectionResult { Encoding = encoding, Confidence = detector.Confidence };
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Ude detected unsupported charset: {Charset}", detector.Charset);
            }
        }

        // Step 3: Try UTF-8 validation
        if (IsValidUtf8(fileBytes))
        {
            _logger.LogInformation("Validated as UTF-8");
            return new DetectionResult { Encoding = Encoding.UTF8, Confidence = 0.7 };
        }

        // Step 4: Default to Shift_JIS for Japanese environments
        _logger.LogWarning("Could not reliably detect encoding, defaulting to Shift_JIS");
        return new DetectionResult
        {
            Encoding = Encoding.GetEncoding("Shift_JIS"),
            Confidence = 0.5
        };
    }

    /// <summary>
    /// Detect encoding from Byte Order Mark (BOM)
    /// </summary>
    private Encoding? DetectEncodingFromBOM(byte[] bytes)
    {
        if (bytes.Length < 2)
            return null;

        // UTF-32 BE (00 00 FE FF)
        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
            return new UTF32Encoding(bigEndian: true, byteOrderMark: true);

        // UTF-32 LE (FF FE 00 00)
        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
            return new UTF32Encoding(bigEndian: false, byteOrderMark: true);

        // UTF-8 BOM (EF BB BF)
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        // UTF-16 BE (FE FF)
        if (bytes[0] == 0xFE && bytes[1] == 0xFF)
            return new UnicodeEncoding(bigEndian: true, byteOrderMark: true);

        // UTF-16 LE (FF FE)
        if (bytes[0] == 0xFF && bytes[1] == 0xFE)
            return new UnicodeEncoding(bigEndian: false, byteOrderMark: true);

        return null;
    }

    /// <summary>
    /// Validate if bytes represent valid UTF-8
    /// </summary>
    private bool IsValidUtf8(byte[] bytes)
    {
        try
        {
            var decoder = Encoding.UTF8.GetDecoder();
            decoder.Fallback = new DecoderExceptionFallback();

            int charCount = decoder.GetCharCount(bytes, 0, bytes.Length, flush: true);
            return charCount > 0;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    /// <summary>
    /// Result of encoding conversion
    /// </summary>
    public class ConversionResult
    {
        public byte[] ConvertedBytes { get; set; } = Array.Empty<byte>();
        public string DetectedEncoding { get; set; } = string.Empty;
        public string OutputEncoding { get; set; } = string.Empty;
        public double? DetectionConfidence { get; set; }
    }

    /// <summary>
    /// Result of encoding detection
    /// </summary>
    private class DetectionResult
    {
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public double Confidence { get; set; }
    }
}
