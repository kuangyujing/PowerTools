# File Encoding Converter API - Power Platform Integration Guide

This guide explains how to use the File Encoding Converter API from Power Apps and Power Automate via custom connectors.

## Table of Contents
- [API Overview](#api-overview)
- [Custom Connector Setup](#custom-connector-setup)
- [Power Apps Implementation](#power-apps-implementation)
- [Power Automate Implementation](#power-automate-implementation)
- [API Reference](#api-reference)

---

## API Overview

The Encoding Converter API converts text files from one character encoding to another. It supports:
- **Automatic encoding detection** when input encoding is not specified
- **Base64-based file transfer** (compatible with Power Platform custom connectors)
- **Multiple encodings**: UTF-8, Shift_JIS, EUC-JP, ISO-2022-JP, and more

### Key Features
- Works with any plain text file (CSV, TXT, JSON, XML, HTML, etc.)
- Auto-detects input encoding using statistical analysis and BOM detection
- Returns converted file in the same format with specified encoding
- Includes confidence score for auto-detection results

---

## Custom Connector Setup

### Step 1: Export OpenAPI Definition

1. Run the PowerTools API locally:
   ```bash
   dotnet run --project PowerTools.Server
   ```

2. Navigate to the Swagger UI:
   ```
   https://localhost:7XXX/swagger
   ```

3. Download the OpenAPI JSON definition:
   ```
   https://localhost:7XXX/swagger/v1/swagger.json
   ```

### Step 2: Create Custom Connector in Power Platform

1. Go to [Power Apps](https://make.powerapps.com) or [Power Automate](https://make.powerautomate.com)
2. Navigate to **Data** → **Custom Connectors**
3. Click **+ New custom connector** → **Import an OpenAPI file**
4. Upload the downloaded `swagger.json` file
5. Configure the connector:
   - **General Tab**:
     - Name: `PowerTools Encoding Converter`
     - Host: `your-api-domain.com` (or `localhost:7XXX` for testing)
     - Base URL: `/`
   - **Security Tab**:
     - Authentication type: `No authentication` (or configure as needed)
   - **Definition Tab**:
     - The actions should be auto-imported from OpenAPI
   - **Test Tab**:
     - Create a connection and test the API

6. Click **Create connector**

### Step 3: Create Connection

1. After creating the connector, click **+ New connection**
2. Complete authentication (if configured)
3. The connection is now ready to use in Power Apps and Power Automate

---

## Power Apps Implementation

### Scenario 1: Convert File and Save to Dataverse

This example shows how to:
1. Upload a file using the attachment control
2. Convert the file encoding
3. Save the converted file to a Dataverse table's file column

#### App Structure

**Controls:**
```
- FileInput1 (Attachment control)
- ddOutputEncoding (Dropdown)
- btnConvert (Button)
- btnSave (Button)
- lblStatus (Label)
```

#### Setup Dropdown for Encodings

**ddOutputEncoding.Items:**
```javascript
Table(
    {Value: "UTF-8", DisplayName: "UTF-8"},
    {Value: "Shift_JIS", DisplayName: "Shift_JIS (Japanese)"},
    {Value: "EUC-JP", DisplayName: "EUC-JP (Japanese)"},
    {Value: "ISO-2022-JP", DisplayName: "ISO-2022-JP (Japanese)"},
    {Value: "GB2312", DisplayName: "GB2312 (Simplified Chinese)"},
    {Value: "Big5", DisplayName: "Big5 (Traditional Chinese)"}
)
```

#### Convert File Button

**btnConvert.OnSelect:**
```javascript
// Convert the uploaded file to the selected encoding
Set(
    varConvertedFile,
    'PowerTools Encoding Converter'.ConvertFileEncoding({
        fileContentBase64: Base64(FileInput1.SelectedFile),
        fileName: FileInput1.SelectedFile.Name,
        outputEncoding: ddOutputEncoding.Selected.Value
        // inputEncoding is optional - omit for auto-detection
    })
);

// Display conversion results
If(
    !IsBlank(varConvertedFile),
    Set(
        lblStatus.Text,
        "Conversion successful!" & Char(10) &
        "Detected: " & varConvertedFile.detectedEncoding & Char(10) &
        "Output: " & varConvertedFile.outputEncoding & Char(10) &
        "Size: " & Round(varConvertedFile.fileSizeBytes / 1024, 2) & " KB" &
        If(
            !IsBlank(varConvertedFile.detectionConfidence),
            Char(10) & "Confidence: " & Text(varConvertedFile.detectionConfidence * 100, "0.0") & "%",
            ""
        )
    );
    Notify("Conversion completed successfully!", NotificationType.Success),
    Notify("Conversion failed!", NotificationType.Error)
);
```

#### Save to Dataverse Button

**btnSave.OnSelect:**
```javascript
// Save the converted file to a Dataverse record
Patch(
    YourDataverseTable,
    LookUp(YourDataverseTable, ID = varRecordId),
    {
        FileColumn: {
            FileName: varConvertedFile.fileName,
            Value: varConvertedFile.fileContentBase64
        },
        Description: "Converted from " & varConvertedFile.detectedEncoding &
                     " to " & varConvertedFile.outputEncoding
    }
);

Notify("File saved to Dataverse!", NotificationType.Success);

// Clear the form
Reset(FileInput1);
Set(varConvertedFile, Blank());
```

### Scenario 2: Download Converted File

If you want to download the converted file instead of saving to Dataverse:

**btnDownload.OnSelect:**
```javascript
// Use the Download function (requires Power Apps experimental features)
Download(
    varConvertedFile.fileContentBase64,
    varConvertedFile.fileName
);
```

### Scenario 3: Specify Input Encoding

If you know the input encoding and don't want auto-detection:

**btnConvert.OnSelect:**
```javascript
Set(
    varConvertedFile,
    'PowerTools Encoding Converter'.ConvertFileEncoding({
        fileContentBase64: Base64(FileInput1.SelectedFile),
        fileName: FileInput1.SelectedFile.Name,
        outputEncoding: ddOutputEncoding.Selected.Value,
        inputEncoding: "Shift_JIS"  // Specify input encoding
    })
);
```

### Full Power Apps Formula Reference

**Get list of supported encodings:**
```javascript
Set(
    varEncodings,
    'PowerTools Encoding Converter'.GetSupportedEncodings()
);

// Use in dropdown
ddOutputEncoding.Items = varEncodings.encodings
```

**Error Handling:**
```javascript
If(
    IsError('PowerTools Encoding Converter'.ConvertFileEncoding({...})),
    Notify(
        "Error: " & FirstError.Message,
        NotificationType.Error
    ),
    // Success logic
    Notify("Success!", NotificationType.Success)
);
```

---

## Power Automate Implementation

### Scenario 1: Auto-Convert SharePoint Files

This flow automatically converts files uploaded to SharePoint to UTF-8 encoding.

**Trigger:**
- **When a file is created (SharePoint)**
  - Site Address: `https://yoursite.sharepoint.com/sites/yoursite`
  - Library Name: `Documents`

**Action 1: Get file content**
- **Get file content (SharePoint)**
  - File Identifier: `@{triggerOutputs()?['body/{Identifier}']}`

**Action 2: Convert encoding**
- **PowerTools Encoding Converter - ConvertFileEncoding**
  - fileContentBase64: `@{base64(body('Get_file_content'))}`
  - fileName: `@{triggerOutputs()?['body/{FilenameWithExtension}']}`
  - outputEncoding: `UTF-8`
  - inputEncoding: _(leave empty for auto-detection)_

**Action 3: Save converted file**
- **Create file (SharePoint)**
  - Site Address: `https://yoursite.sharepoint.com/sites/yoursite`
  - Folder Path: `/Converted`
  - File Name: `@{body('PowerTools_Encoding_Converter_-_ConvertFileEncoding')?['fileName']}`
  - File Content: `@{base64ToBinary(body('PowerTools_Encoding_Converter_-_ConvertFileEncoding')?['fileContentBase64'])}`

### Scenario 2: Convert and Email

**Action: Send an email with attachment**
```
To: user@example.com
Subject: Converted File - @{body('ConvertFileEncoding')?['fileName']}
Body:
  File converted successfully!
  Original encoding: @{body('ConvertFileEncoding')?['detectedEncoding']}
  New encoding: @{body('ConvertFileEncoding')?['outputEncoding']}

Attachments:
  - Name: @{body('ConvertFileEncoding')?['fileName']}
  - Content Bytes: @{base64ToBinary(body('ConvertFileEncoding')?['fileContentBase64'])}
```

### Scenario 3: Batch Convert Multiple Files

**Apply to each (files from SharePoint folder)**
```
For each: @{body('Get_files_(properties_only)')?['value']}

  Action: Get file content
    File Identifier: @{items('Apply_to_each')?['{Identifier}']}

  Action: Convert encoding
    fileContentBase64: @{base64(body('Get_file_content'))}
    fileName: @{items('Apply_to_each')?['{FilenameWithExtension}']}
    outputEncoding: Shift_JIS

  Action: Create file
    File Name: @{body('Convert_encoding')?['fileName']}
    File Content: @{base64ToBinary(body('Convert_encoding')?['fileContentBase64'])}
```

### Flow JSON Example

```json
{
  "type": "OpenApiConnection",
  "inputs": {
    "host": {
      "connectionName": "shared_powertoolsencodingco_xxxxx",
      "operationId": "ConvertFileEncoding",
      "apiId": "/providers/Microsoft.PowerApps/apis/shared_powertoolsencodingco"
    },
    "parameters": {
      "body": {
        "fileContentBase64": "@{base64(body('Get_file_content'))}",
        "fileName": "@{triggerOutputs()?['body/{FilenameWithExtension}']}",
        "outputEncoding": "UTF-8"
      }
    }
  }
}
```

---

## API Reference

### Endpoint: Convert File Encoding

**Request:**
```http
POST /api/encodingconverter/convert
Content-Type: application/json

{
  "fileContentBase64": "base64_encoded_file_content",
  "fileName": "example.csv",
  "outputEncoding": "UTF-8",
  "inputEncoding": "Shift_JIS"  // Optional
}
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "fileContentBase64": "converted_base64_content",
  "fileName": "example.csv",
  "detectedEncoding": "shift_jis",
  "outputEncoding": "utf-8",
  "fileSizeBytes": 12345,
  "detectionConfidence": 0.95  // Only when auto-detecting
}
```

### Endpoint: Get Supported Encodings

**Request:**
```http
GET /api/encodingconverter/encodings
```

**Response:**
```json
{
  "encodings": [
    { "name": "UTF-8", "displayName": "UTF-8" },
    { "name": "Shift_JIS", "displayName": "Shift_JIS (Japanese)" },
    { "name": "EUC-JP", "displayName": "EUC-JP (Japanese)" },
    ...
  ]
}
```

### Supported Encodings

| Encoding | Description | Common Use |
|----------|-------------|------------|
| UTF-8 | Unicode (8-bit) | Modern standard, web |
| UTF-16 | Unicode (16-bit LE) | Windows, .NET |
| UTF-16BE | Unicode (16-bit BE) | Unix, Mac |
| Shift_JIS | Japanese | Legacy Japanese files |
| EUC-JP | Japanese | Unix Japanese files |
| ISO-2022-JP | Japanese | Email (JIS) |
| GB2312 | Simplified Chinese | Chinese files |
| Big5 | Traditional Chinese | Taiwan, Hong Kong |
| EUC-KR | Korean | Korean files |
| ISO-8859-1 | Latin-1 | Western European |
| Windows-1252 | Windows Latin | Windows Western European |

### Error Responses

**400 Bad Request:**
```json
{
  "error": "Invalid Base64 file content"
}
```

**400 Bad Request:**
```json
{
  "error": "Binary files are not supported. Only text files can be converted."
}
```

**400 Bad Request:**
```json
{
  "error": "Unsupported output encoding: XYZ"
}
```

**500 Internal Server Error:**
```json
{
  "error": "An error occurred during file conversion",
  "details": "Error message details"
}
```

---

## Best Practices

### 1. File Size Considerations
- For files larger than 10MB, consider using chunked processing
- Base64 encoding increases payload size by ~33%
- Power Automate has a 100MB limit for action inputs/outputs

### 2. Encoding Detection
- Auto-detection works best with files > 1KB
- For critical applications, specify input encoding if known
- Check `detectionConfidence` - values below 0.8 may be unreliable

### 3. Error Handling
- Always implement error handling in your flows/apps
- Check for `IsError()` in Power Apps
- Use "Configure run after" in Power Automate to handle failures

### 4. Performance
- Cache the list of supported encodings in a collection
- Use parallel processing for batch conversions in Power Automate
- Consider using the "Apply to each" concurrency settings for better performance

### 5. Security
- Validate file types before conversion
- Implement authentication on the API if handling sensitive data
- Use environment variables for API endpoints

---

## Troubleshooting

### Issue: "Invalid Base64 file content"
**Solution:** Ensure you're using `Base64()` function in Power Apps or `base64()` expression in Power Automate.

### Issue: "Binary files are not supported"
**Solution:** The API only supports text files. Check file type before conversion.

### Issue: Low detection confidence
**Solution:** Specify the input encoding explicitly or ensure file has enough content for accurate detection.

### Issue: Custom connector not showing in Power Apps
**Solution:** Ensure you've created a connection after creating the custom connector.

### Issue: "Could not execute request"
**Solution:** Verify API endpoint is accessible and authentication is configured correctly.

---

## Additional Resources

- [Power Platform Custom Connectors Documentation](https://learn.microsoft.com/en-us/connectors/custom-connectors/)
- [Power Apps Formula Reference](https://learn.microsoft.com/en-us/powerapps/maker/canvas-apps/formula-reference)
- [Power Automate Expression Reference](https://learn.microsoft.com/en-us/power-automate/use-expressions-in-conditions)

---

## Support

For issues or questions:
1. Check the [PowerTools GitHub repository](https://github.com/yourorg/powertools)
2. Review API logs for error details
3. Test the API directly using Swagger UI before troubleshooting connector issues
