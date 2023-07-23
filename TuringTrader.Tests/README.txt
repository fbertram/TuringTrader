These are the tests used to validate the build. Unfortunately, these tests
won't run for everybody, due to some requirements/ dependencies:

___v1___
* these tests are a little sparse and generally the v2 engine has
  more complete test coverage.
* as the v1 engine expresses all timestamps in the exchange's timezone,
  many tests rely on the local date to match the exchange's date.

___v2___
* many tests only run on Norgate Data. At some point in the future,
  it might be possible to remove this dependency and run the tests
  on data backfills instead - but this is not a priority right now.
* many indicator-related tests compare results from the v2 indicators
  with the results from v1. For these tests, the same restrictions
  as for the v1 engine apply - in addition to the v2 restrictions.
* some tests check for the execution time. These tests might fail
  on slow computers, and if multiple tests are running in parallel.
