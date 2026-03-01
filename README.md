# Claude_Code_Revit_Test

## Pending Work

- [ ] Create `tests/PDG.Revit.AutomationTools.Tests/` with at minimum one pure-logic
      [Fact] test per service, mirroring the pattern in
      `tests/PDG.Revit.FireRatingLines.Tests/FireRatingLinesServiceTests.cs`.
      Revit-dependent tests must be marked [Fact(Skip = "Requires live Revit")].