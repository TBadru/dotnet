﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Test.Cohost.Formatting;

[Collection(HtmlFormattingCollection.Name)]
public class HtmlFormattingPassTest(FormattingTestContext context, HtmlFormattingFixture fixture, ITestOutputHelper testOutput)
    : FormattingTestBase(context, fixture.Service, testOutput), IClassFixture<FormattingTestContext>
{
    [Theory]
    [WorkItem("https://github.com/dotnet/razor/issues/11846")]
    [InlineData("", "")]
    [InlineData("$", "")]
    [InlineData("", "u8")]
    [InlineData("$", "u8")]
    [InlineData("@", "")]
    [InlineData("@$", "")]
    [InlineData(@"""""""", @"""""""")]
    [InlineData(@"$""""""", @"""""""")]
    [InlineData(@"""""""\r\n", @"\r\n""""""")]
    [InlineData(@"$""""""\r\n", @"\r\n""""""")]
    [InlineData(@"""""""", @"""""""u8")]
    [InlineData(@"$""""""", @"""""""u8")]
    [InlineData(@"""""""\r\n", @"\r\n""""""u8")]
    [InlineData(@"$""""""\r\n", @"\r\n""""""u8")]
    public async Task RemoveEditThatSplitsStringLiteral(string prefix, string suffix)
    {
        var document = CreateProjectAndRazorDocument($"""
            @({prefix}"this is a line that is 46 characters long"{suffix})
            """);
        var change = new TextChange(new TextSpan(24, 0), "\r\n");
        var edits = await GetHtmlFormattingEditsAsync(document, change);
        Assert.Empty(edits);
    }

    private async Task<System.Collections.Immutable.ImmutableArray<TextChange>> GetHtmlFormattingEditsAsync(CodeAnalysis.TextDocument document, TextChange change)
    {
        var documentMappingService = OOPExportProvider.GetExportedValue<IDocumentMappingService>();
        var pass = new HtmlFormattingPass(documentMappingService);

        var snapshotManager = OOPExportProvider.GetExportedValue<RemoteSnapshotManager>();
        var snapshot = snapshotManager.GetSnapshot(document);

        var loggerFactory = new TestFormattingLoggerFactory(TestOutputHelper);
        var logger = loggerFactory.CreateLogger(document.FilePath.AssumeNotNull(), "Html");
        var codeDocument = await snapshot.GetGeneratedOutputAsync(DisposalToken);
        var context = FormattingContext.Create(snapshot,
            codeDocument,
            new RazorFormattingOptions(),
            logger);

        var edits = await pass.GetTestAccessor().FilterIncomingChangesAsync(context, [change], DisposalToken);
        return edits;
    }
}
