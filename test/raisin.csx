await Raisin
    .WithInputDirectory(".")
    .WithOutputDirectory("_output")
    .WithRazorRoot("_theme/root.cshtml")
    .WithMarkdown()
    .WithTableOfContents()
    .WithForceCleanOutput()
    .GenerateAsync(); 
