# Game update validation

- Android C# compilation: production scripts compiled against the existing Unity Android response file.
- ProgressionChecks.cs: isolated tests of the production LevelManager with in-memory PlayerPrefs. 6,009 assertions pass, including progression through 1,000 levels, failed rounds, preservation of stars and five safe-area fit calculations.
- These checks do not simulate Unity rendering, native Android input or persistence on a real device.
- Unity batch validation was attempted outside the sandbox but refused because this project is already open in another Unity instance. See unity-check.log.

## Play-mode/device checklist

1. Adventure: fail level 1, verify level 2 stays locked; earn 50 points, finish the round, verify Next Level opens level 2.
2. Finish level 10, open More Levels, and verify only level 11 is newly available.
3. Ultimate Run: no countdown; each escaped non-bomb costs one heart; a heart balloon restores one (up to five); zero hearts ends the run. Ultimate does not unlock Adventure levels.
4. Pause, Settings, Resume, Home and replay: verify no balloon can be popped through a modal; progress survives relaunch.
5. Check phone portrait, tablet, landscape and a notched Android device. The full 1080 x 1920 design fits the safe area; landscape uses a smaller centered layout.
6. Background and balloon selectors remain available. Check repeated background changes for duplicated scenery.

The changes are source changes; existing APK files have not been rebuilt in this update.
