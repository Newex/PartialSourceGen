# Testing

The testing methodology used is, snapshot testing.

1. Each test, produces a file output.  
2. On first run the output must manually be evaluated and accepted that it produces the desired output.  
3. After acceptance, the subsequent test runs, will compare the generated output to the accepted output and any deviation will result in an error.

To run the tests from the command line execute this: `dotnet test --filter "Category=SnapshotTest"`.

The accepted result can be found in the folder `Results`.

## Debugging

* open your favorite IDE
* put a breakpoint
* run any snapshot test
* or use the debugging test class with your own custom source code input.

# See wiki
https://github.com/Newex/PartialSourceGen/wiki/Snapshot-testing