// Every test class has its own factory and game store, so classes can run side by side (as they did under xUnit).
// Classes that must run alone say so with [NonParallelizable].
[assembly: Parallelizable(ParallelScope.Fixtures)]
