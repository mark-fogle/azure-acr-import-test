// File:  ImportRequest.cs
// Author: Mark Fogle
// Company: ActiGraph
// Created: 2022-07-01
// Purpose:

namespace AcrImportFunctionTest;

public class ImportRequest
{
    public string SourceImageTag { get; set; }
    public string[] DestinationImageTags { get; set; }
}