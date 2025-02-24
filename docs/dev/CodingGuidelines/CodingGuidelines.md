# Coding Guidelines

## Table of Contents

* [Code Style](#code-style)
    * [Naming of Private and Protected Fields](#naming-of-private-and-protected-fields)
    * [Naming of Constants](#naming-of-constants)
    * [Naming of Asynchronous Methods](#naming-of-asynchronous-methods)
    * [Naming of Value Tuples](#naming-of-value-tuples)
    * [Naming of Test Methods](#naming-of-test-methods)
    * [Indentation Depth](#indentation-depth)
    * [Control Flow Statements](#control-flow-statements)
    * [Local Functions](#local-functions)
* [Best Practices](#best-practices)
    * [Diagnostic and Code Fix Messages Style](#diagnostic-and-code-fix-messages-style) 
    * [Analyzers](#analyzers)
        * [Independent Analyzers](#independent-analyzers)
        * [Aggregated analyzers](#aggregated-analyzers)
    * [Unit Tests](#unit-tests)
    * [Cancellation Support](#cancellation-support)
    * [Demo Solution](#demo-solution)
    * [Code Reuse](#code-reuse)
    * [Task Blocking](#task-blocking)
    * [Task Awaiting](#task-awaiting)
    * [Parametrized Diagnostic Messages](#parametrized-diagnostic-messages)
    * [Test Methods](#test-methods)
    * [Async Anonymous Delegates](#async-anonymous-delegates)
    * [Value Tuples](#value-tuples)
    * [Debugging Hints](#debugging-hints)

## Code Style

The [.NET Framework Naming Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines) must be used in addition to our own.

### Naming of Private and Protected Fields

An underscore must be used as a prefix for private and protected fields in classes.

```C#
public class MyClass
{
  private object wronglyNamedField;       //Incorrect naming
 
  protected object _correctlyNamedField;  //Correct naming
}
```

### Naming of Constants

Names of constants must begin with a capital letter.

```C#
private const string SetValueMethodName = "SetValue"; // Correct
private const string _setValueMethodName = "SetValue"; // Incorrect
```

### Naming of Asynchronous Methods

Asynchronous methods must have the `Async` postfix.

```C#
public Task<int> GetCountAsync() { } // Correct
public Task<int> GetCount() { } // Incorrect
public async Task ExecuteAsync() { } // Correct
public async Task Execute() { } // Incorrect
```

### Naming of Value Tuples

The names of the properties in `ValueTuple` must use *PascalCase*, except the case when the tuple is a local variable.

```C#
public (int Line, int Column) GetPosition() { } // Correct
public (int line, int column) GetPosition() { } // Incorrect
```

### Naming of Test Methods

Test methods should be named in *PascalCase* with the underscore character as a separator between logical statements.

```C#
public Task EventHandlersWithExternalMethod(string actual) { } // Valid
public Task EventHandlers_ShouldNotShowDiagnostic(string actual) { } // Valid
public Task TestCodeFix_RowSelected(string actual, string expected) { } // Valid
public Task TestDiagnostic_RowSelected(string actual) { } // Valid if the same class also contains the tests for the code fix

public Task No_Connection_Scope_In_Row_Selected(string actual) { } // Invalid
public Task Test_CodeFix_For_RowSelecting(string actual) { } // Invalid
```

### Indentation Depth

Indentation depth (brace block levels) should be no more than three.

```C#
public void Foo(bool flag, IEnumerable collection)
{
  if (collection != null)
  {
      // some code
 
      if (flag)
      {
        // some code
       
        foreach(object item in collection)
        {
           //Maximum allowed level of indentation
           if (item != null)
           {
              //identation level > 3, invalid
           }
        }
      }
   }
}
```

### Control Flow Statements

Control flow statements (such as `if `, `while`, `for`, `foreach`, `switch`, `do`-`while`) should be separated with empty lines.

```C#
public bool Foo(bool flag)
{
    DoSomething();

    if (flag)
        return true;

    return false;
}
```

A logical grouping of the code can be an exception to this rule.

```C#
public bool Foo(bool flag)
{
    DoSomething();

    bool condition = flag & GetStatus();
    if (condition)
        return true;

    return false;
}
```

### Local Functions

The local functions from C# 7 should be used with caution. A general recommendation is to not overuse this feature.

The local functions can be used in the following three cases:

* If you implement a generator method with the argument check that is made immediately.

  ```C#
  public IEnumerable<int> Generator(string parameter)
  {
      if (parameter == null)
         throw new ArgumentNullException(nameof(parameter));   //The check is performed immediately.
    
      return GeneratorInternal();
 
       IEnumerable<int> GeneratorInternal()
       {
           int i = parameter == "Y" ? 1 : 0;
          yield return i; 
       }
  }
  ```

* If you implement an async method with the argument check.
* If you need better grouping of the public method with the private methods that only this public method uses. The general .NET convention recommends that you put all public methods above the private ones. However, you can improve readability by grouping private methods that are related to only one public method as its local functions. The number of these local functions should be no more than three.

## Best Practices

The [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions) should be considered in addition to our own.

### Diagnostic and Code Fix Messages Style

Diagnostic and code fix messages should follow the following rules:
* All messages must be approved by Acumatica. Acumatica documentation team is responsible for the verification of new messages.

* There are rules for the placement dot at the end of the sentence. In this area Acuminator messages are consistent with Microsoft compiler warning messages:
   * For messages consisting of a single sentence do not place dot at the end of the sentence
   * For messages consisting of two or more sentences place dot at the end of each sentence

### Analyzers

Overall Acuminator analyzers can be divided into two categories:
* Independent analyzers
* Aggregated analyzers

#### Independent Analyzers

Independent analyzers are analyzers which cannot be easily grouped together. This happens because the checks performed by independant analyzers are applied to code elements that have little in common. This feature makes any usefull code or data sharing impossible. 
For example, one independent analyzer checks everywhere in code that the number of arguments passed to a BQL query corresponds to the number of parameters declared by the query. Another independent analyzer checks everywhere in code that graphs are not instantiated with a constructor. These two analyzers have nothing to share with each other.

Independent analyzers should be derived from the `PXDiagnosticAnalyzer` class. This class provides base support for the Acuminator infrastructure, unit tests, and some general checks. It also intializes derived analyzers and supplies them with the following data:
* Acuminator code analysis settings. It is a collection of flags which indicates such features as whether the code analysis is enabled, or whether the ISV mode is enabled. See the `CodeAnalysisSettings` class declaration for more details.
* The `PXContext` object. It is a wrapper over Roslyn `Compilation` which contains the Roslyn `ISymbol` symbols specific to Acumatica ERP. If you need to add a new symbol you should add it to `PXContext` or to one of its collection of symbols.  

The `PXDiagnosticAnalyzer` class also provides several extensibility points. The most useful extensibility points are the following:
* The `bool ShouldAnalyze(PXContext pxContext)` method which is responsible for a general check if the analyzer should run a diagnostic. For example, if some check requires an Acumatica ERP class or method to be defined, you can make a check that a corresponding Roslyn symbol is not null in this method. Do not forget to call the base method in your overrides because the method performs several very important general checks.
* The `void AnalyzeCompilation(CompilationStartAnalysisContext compilationStartContext, PXContext pxContext)` abstract method. In this method, you should put your analyzer's logic.

To support Acuminator unit tests infrastructure, all independent analyzers must declare the following two constructors:
* A constructor which accepts the `CodeAnalysisSettings` parameter
* A default constructor which is chained to the constructor with the `CodeAnalysisSettings` parameter

An example of correct constructor declaration is shown in the following code.
```C#
public BqlParameterMismatchAnalyzer() : this(null){ }

public BqlParameterMismatchAnalyzer(CodeAnalysisSettings codeAnalysisSettings) : base(codeAnalysisSettings){ }
```
The default constructor is required by the Roslyn infrastructure. The other constructor is a constructor for unit tests which allows them to specify custom code analysis settings.

#### Aggregated analyzers

Usually, a new Acuminator diagnostic should be applied only to one of the following types of objects: 
* Graphs and their extensions
* DACs and their extensions
* Event handlers in graphs, graph extensions, attributes, and some specialized helper classes

When a diagnostic can be applied to one of these objects, you do not need to implement an independent analyzer. You can create an aggregated analyzer which is simpler. Aggregated analyzers are Acuminator-specific analyzers which have one thing in common, that is, a type of object they analyze. 
This allows aggregated analyzers from the same aggregation group to share and reuse a lot of common information about the analyzed object. Acuminator has the following groups of aggregated analyzers:
* Graph analyzers
* DAC analyzers
* Event handler analyzers

From the Roslyn point of view, aggregated analyzers are not different from a normal C# class. Acuminator is responsible for their execution and sharing of data between them. For each group of aggregated analyzers there is one "aggregator" analyzer:
* `PXGraphAnalyzer` for the analysis of graphs and graph extensions. It tries to obtain a graph semantic model (the `PXGraphSemanticModel` object) for a graph or graph extension type. In case of success, all aggregated graph analyzers are executed and each analyzer receives the calculated graph semantic model.
* `DacAnalyzersAggregator` for the analysis of DACs and DAC extensions. It tries to obtain a DAC semantic model (the `DacSemanticModel` object) for a DAC or DAC extension type. In case of success, all aggregated DAC analyzers are executed and each analyzer receives the calculated DAC semantic model.
* `EventHandlerAnalyzer` for the analysis of event handlers. It works with methods and tries to obtain a type of graph event a method represents. If the method represents some graph event, then all aggregated event handler analyzers are executed on the method and each analyzer receives the calculated type of graph event.

Aggregator analyzers can be considered to be independent analyzers. They are derived from `PXDiagnosticAnalyzer` and are normal Roslyn analyzers with a collection of aggregated analyzers responsible for independent subchecks.
Their logic is very simple: An aggregator collects all information shared between aggregated analyzers from the code, performs some shared checks on the code, and then concurrently calls all aggregated analyzers passing them the collected data.
Each aggregator stores a list of aggregated analyzers. All new aggregated analyzers must be added to the list of aggregated analyzers of the corresponding aggregator.

The aggregated analyzers should be either derived from one of the following base types or implement a corresponding interface:
* For graph analyzers, the base type is `PXGraphAggregatedAnalyzerBase`, and the interface is `IPXGraphAnalyzer`
* For DAC analyzers, the base type is `DacAggregatedAnalyzerBase`, and the interface is `IDacAnalyzer`
* For event handler analyzers, the base type is `EventHandlerAggregatedAnalyzerBase`, and the interface is `IEventHandlerAnalyzer`

The base classes provide a default implementation for the corresponding interfaces. Usually this is enough but sometimes you may need to implement an analyzer which can be attributed to more than one group. In this case you can implement corresponding interfaces directly.

Acuminator includes the following extensibility points for aggregated analyzers:
* The `ShouldAnalyze` method which determines whether the analyzer shoud be executed on the input object. This method accepts shared information passed to the analyzer as parameter which allows it to make more specialized checks.
* The `Analyze` method which accepts shared information passed to the analyzer. You should place your analyzer's logic in this method.

Note that unlike independent analyzers, aggregated analyzers do not need any constructors to be declared.

### Unit Tests

Each analyzer, code fix, or refactoring should be covered with unit tests. To support Acuminator unit tests infrastructure, all independent Acuminator analyzers and code refactorings must have the following two constructors:
* A constructor accepting the `CodeAnalysisSettings` parameter
* A default constructor which is chained to the constructor with the `CodeAnalysisSettings` parameter

All unit tests must use the constructor which accepts code analysis settings, to create an analyzer or refactoring provider. Each unit test must specify explicitly all code analysis settings that could affect its result. 
The tests should be independent of default code analysis settings values. For all unit tests, you should specify that the static code analysis is enabled and suppression mechanism is disabled. Example of a unit test is shown in the following code.

```C#
public class ThrowingExceptionsInEventHandlersTests : DiagnosticVerifier
{
	protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
		new EventHandlerAnalyzer(CodeAnalysisSettings.Default.WithStaticAnalysisEnabled()
									 .WithSuppressionMechanismDisabled(),
			new ThrowingExceptionsInEventHandlersAnalyzer());

//The rest of the code
}
```
You do not need to specify other settings if they do not affect the test execution.

### Cancellation Support

The cancellation support should be added to Acuminator diagnostics and code fixes.

This means that the cancellation token, which is passed to every Roslyn diagnostic and code fix, should be checked between big calculation steps. Also, the token should be passed to every Roslyn framework method that supports cancellation, such as `Document.GetSyntaxRootAsync()`.

The check for cancellation via cancellation token must be done using the following construction.

```C#
cancellationToken.ThrowIfCancellationRequested();
```

There is one exception to this rule. Do not pass cancellation token to the `ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()` in the VSIX project. This method has overload that accepts cancellation.
However, it doesn't cancel the operation. It just cancels the transition to the UI thread if it weren't already done scheduling the continuation on a threadpool. This is not expected by our code and should be avoided.

### Demo Solution

If you add a new diagnostic or other functionality to the Acuminator, you also should add corresponding examples to the demo solution, which is located under the */src/Samples* folder.

We use the demo solution for the following two purposes:

1. As a lightweight Visual Studio solution for debugging.
2. As a demo of Acuminator. 

Because the solution is used for demo purposes, we need to maintain it in the consistent state — that is, it should compile without errors, and it should contain examples that are similar to the real business cases.

### Code Reuse

You should try to reuse existing Acuminator's helper methods. The Acuminator code already includes the utilities for many common tasks related to the Acumatica-specific objects, such as the following:

* The `PXGraphSemanticModel` semantic model for graphs and graph extensions. It is a container with information about graph/graph extension: graph events, views and view delegates, action and action delegates, etc.
* The `DacSemanticModel` semantic model for DACs and DAC extensions. It is a container with information about DAC/DAC extension: DAC properties, DAC fields, etc.
* The utility for the [recursive code analysis](../RecursiveCodeAnalysis/RecursiveCodeAnalysis.md)
* The utility that checks whether the type is an Acumatica-specific type, such as a DAC, DAC extension, graph, graph extension, view, BQL statement, or PXAction
* The utility that obtains the views or actions from a graph or graph extension
* The utility that obtains the field attributes from a DAC property and finds out whether the field is bound or unbound
* Syntax manipulation helpers, such as a helper that gets the next statement node in a method
* Semantic helpers

When you write a piece of functionality for a diagnostic (such as a generic check), you should add the `Acuminator.Utilities` namespace to the list of `using` directives and review the existing helpers in the `Acuminator.Utilities` namespace. If there are no helpers appropriate for your task, you should add a new helper to the `Acuminator.Utilities` project.

### Task Blocking

You should avoid the use of `Task.Result` and `Task.Wait()` because this can cause deadlocks and thread pool exhaustion. You should consider using `ThreadHelper.JoinableTaskFactory.Run()` instead.

For details, see the following articles: 

* [How to: Manage multiple threads in managed code](https://docs.microsoft.com/en-us/visualstudio/extensibility/managing-multiple-threads-in-managed-code) 
* [Asynchronous and multithreaded programming within VS using the JoinableTaskFactory](https://blogs.msdn.microsoft.com/andrewarnottms/2014/05/07/asynchronous-and-multithreaded-programming-within-vs-using-the-joinabletaskfactory/)
* [Cookbook for Visual Studio](https://github.com/Microsoft/vs-threading/blob/master/doc/cookbook_vs.md)
* [Three Threading Rules](https://github.com/Microsoft/vs-threading/blob/master/doc/threading_rules.md)

### Task Awaiting

You should use `Task.ConfigureAwait(false)` within analyzers, code fixes, and refactorings to improve the performance and avoid potential deadlocks.

The use of `ConfigureAwait(false)` in other places (such as VSIX) without additional statements, such as `await TaskScheduler.Default`, should be avoided. For more information, see [Cookbook for Visual Studio](https://github.com/Microsoft/vs-threading/blob/master/doc/cookbook_vs.md).

### Parametrized Diagnostic Messages

If a message for a diagnostic must be parameterized (such as "The {0} field cannot be declared within a DAC declaration"), this parametrized message should be passed to the diagnostic descriptor in the `MessageFormat` property instead of the `Title` property. The `Title` property must contain a brief description without parameters.

This rule is described inside the `DiagnosticDescriptor` class, which fragment is shown in the following code.

```C#
/// <summary>A short localizable title describing the diagnostic.</summary>
public LocalizableString Title { get; }
 /// <summary>
/// A localizable format message string, which can be passed as the first argument to <see cref="M:System.String.Format(System.String,System.Object[])" /> when creating the diagnostic message with this descriptor.
/// </summary>
/// <returns></returns>
public LocalizableString MessageFormat { get; }
```

You can set the `MessageFormat` property by using an optional parameter during creation of a new `DiagnosticDescriptor` instance, as shown in the following code.

```C#
public static DiagnosticDescriptor PX1027_ForbiddenFieldsInDacDeclaration { get; } =
    Rule("PX1027", nameof(Resources.PX1027Title).GetLocalized(), Category.Default, DiagnosticSeverity.Error,
    nameof(Resources.PX1027MessageFormat).GetLocalized()); // MessageFormat
```

### Test Methods

Test methods should be asynchronous. This improves overall test performance and avoids potential deadlocks for test runners.

```C#
public Task TestDiagnostic(string actual) => VerifyCSharpDiagnosticAsync(actual);
```

You should use the `async` / `await` pattern to avoid wrapping exceptions in `AggregateException` and make the test output more readable.

```C#
public async Task TestDiagnostic(string actual) => await VerifyCSharpDiagnosticAsync(actual);
```

### Async Anonymous Delegates

You must never pass the asynchronous methods that have the `void` return type to the `analyzer.RegisterXXX` API methods. 

Internally Roslyn diagnostic engine catches `OperationCancelledException` exceptions. However, in .NET, the asynchronous methods that return `void` handle exceptions differently than the asynchronous methods that return a `Task`. The latter methods attach the aggregated exception to the `Task` while the methods that return `void` report the exception to `SynchronizationContext`. In `SynchronizationContext`, the exception becomes unhandled and silently crashes Visual Studio. 

These bugs have concurrent nature because they appear only during the cancellation of the wrongly-written analyzer and are very hard to reproduce and debug.
Therefore, such methods prevent Visual Studio from catching `OperationCancelledException` and must never be passed to the analyzer API. 
The latest version of Roslyn even has a runtime check for async methods.

The following code produces this bug.

```C#
//WRONG
compilationStartContext.RegisterSymbolAction(Analyze_Something_Async, SymbolKind.NamedType);

private static async void Analyze_Something_Async(SymbolAnalysisContext symbolContext) 
{
    //Analysis
}
```

It is especially easy to introduce this bug with async delegates because `analyzer.RegisterXXX` API methods accept `Action<T>` delegates that return `void`, but do not accept `Func<T, Task>`. For example, the following code produces the bug.

```C#
//WRONG
compilationStartContext.RegisterSymbolAction(async symbolContext => await Analyze_Something_Async(symbolContext, pxContext),
                                             SymbolKind.NamedType);
```

So, how to write analyzers correctly? Do not pass asynchronous methods to the `analyzer.RegisterXXX` methods at all. 

Roslyn has both synchronous and asynchronous API methods, such as `symbol.GetSyntax(CancellationToken)` and `symbol.GetSyntaxAsync(CancellationToken)`. The analysis is executed in a background thread, therefore the 
use of the synchronous API shouldn't be a big deal.

```C#
//CORRECT (Pay no attention to the VS warning about a not awaited task in this case)
compilationStartContext.RegisterSymbolAction(symbolContext => Analyze_Something_Sync(symbolContext, pxContext),
                                             SymbolKind.NamedType);
```

There is also an option to pass the asynchronous methods that return a `Task`. However we do not recommend to do like this. 
The Roslyn diagnostic engine does not expect any return objects, so the returned `Task` won't be ever awaited. 
You also cannot await it because this will lead to the creation of the async delegate as described above. And if the cancellation happens the returned `Task`
will become 'unobserved' and may throw an exception in the finalization thread (`Task` is `IDisposable`). Such unobserved `Task`s can be swallowed 
but this depends on the configuration of the user runtime. For more details, see
https://docs.microsoft.com/ru-ru/dotnet/framework/configure-apps/file-schema/runtime/throwunobservedtaskexceptions-element.

Overall, the passing of asynchronous methods to `analyzer.RegisterXXX `is an unwanted scenario that should be avoided at all costs.

### Value Tuples

There is an issue with value tuples in Visual Studio 2017. There is a dependency conflict between some packages depending on different versions of `System.ValueTuple` which proved to be impossible to fix.
The issue appear as a `MissingMethod` exception being thrown on attempt to call public API method with signature containing value tuples if the caller and callee are declared in different assemblies.
For example, consider a method `TryAwait` (see code below) declared in Acuminator.Utilities:

```C#
public Task<(bool IsSuccess, TResult Result)> TryAwait<TResult>(this Task<TResult> task);
```

If you call it from Acuminator.Vsix then in Visual Studio 2017 the `MissingMethod` exception will be thrown. However, everything will be ok in Visual Studio 2019.
Therefore, until we drop the support for Visual Studio 2017, do not declare public API containing value tuples. Either declare it as internal API or create custom structs for it.

### Debugging Hints

There is an unobvious issue with debugging observed on the latest versions of Visual Studio 2019 (starting from 16.8.0). The breakpoints set inside Roslyn analyzers are not hit for no obvious reason. The root cause of this problem lies in a new Visual Studio perfomance optimization which moves execution of all Roslyn analyzers out of the Visual Studio process into a separate 64-bit process. This action is regulated by the  following Visual Studio setting: *Tools -> Options -> Text Editor -> C# -> Advanced -> Use 64-bit process for code analysis*.
You need to do one of the following:
* Disable this setting in an experimental instance of Visual Studio.
* Perform a multiprocess debugging by attaching your debugger to a second process with loaded Roslyn analyzers. The name of the process should look like the following: *ServiceHub.RoslynCodeAnalysisService.exe*.

The first option is much simpler but you have to check that your diagnostic works correctly when the out of process analysis is enabled for Visual Studio.
