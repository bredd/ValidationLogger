// See https://aka.ms/new-console-template for more information
using Bredd.CodeBit;

var vl = new ValidationLogger(ValidationLevel.All);
vl.Log(ValidationLevel.Trace, "Test", "Starting log.");
vl.Log(ValidationLevel.Debug, "Test", "Debug message");
using (vl.BeginScope("Scope1")) {
    vl.Log(ValidationLevel.Information, "InScope", "At the information level.");
    vl.Log(ValidationLevel.Warning, "Something", "Danger, Will Robinson!");
    using (vl.BeginScope("Scope2")) {
        vl.Log(ValidationLevel.Error, "CPU", "CPU Failure imminent.");
    }
    vl.Log(ValidationLevel.Trace, "Test", "Scope2 Ended");
}
vl.Log(ValidationLevel.Trace, "Test", "Scope1 Ended");
using (vl.BeginScope("SomeScope")) {
    vl.Log(ValidationLevel.Error, "Outer", "You have reached the outer limits");
}
vl.Log(ValidationLevel.Trace, "Test", "Ending log.");

Console.Write(vl.ToString());
