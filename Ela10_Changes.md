## Introduction ##

This article contains an overview of breaking changes introduced in Ela 0.10.


## No mutable data structures ##

Ela 0.10 is a pure language and support for mutable data structures on a language level is dropped. Now it is not possible to declare a record with mutable fields, e.g. syntax like so:

```
let rec = { !x = 0 } 
```

is no longer valid.

Also assignment operator `(<-)` is no longer supported.
However mutation of state can be done through library support. Ela 0.10 comes with a standard `Cell` module that implements reference cells in OCaml style.

Mutation of data (Ela 0.9):

```
let rec = { !value = 0 }
rec.value <- 1 $
rec.value 
```

Mutation of data (Ela 0.10):

```
open Cell

let c = cell 0
c |> mutate 1 $
valueof c
```

Some syntax sugar is also available. An example above can be rewritten using a `(:=)` operator defined in `Cell` module like so:

```
open Cell

let c = cell 0 
c := 1 $
valueof c
```

Operators `(.+)` and `(.-)` can be used to perform increment and decrement as in C style languages:

```
let c = cell 0
c.+ $
valueof c //value of c is 1
```

## Indexing ##

Indexing syntax (e.g. `foo.[bar]`) is no longer supported. A new indexing operator `(:)` is added to standard prelude. One can use it in all the cases where indexing operator was used.

Indexing (Ela 0.9):

```
let xs = [1..10]
xs.[0]
```

Indexing (Ela 0.10):

```
let xs = [1..10]
xs:0
```

The same `(:)` would work for indices of any type:

```
let rec = {x=1,y=2}
rec:"y"
```


## Name shadowing ##

Ela 0.10 supports name shadowing when names are declared in the same scope. The following code is now correct (used to produce compile errors):

```
let x = 0
let x = 1

let foo = \x x -> x*x
```

It is important to understand that a lambda declaration `\x x -> x*x` doesn't contain a non-linear pattern (like in Prolog). Ela is a functional, not a logical language. And the lambda above is defined according to the rules of lambda calculus - it is fully equivalent to: `\x -> \x -> x*x`. The same works for named functions as well.


## Application operators ##

Operators `(|>)` and `(<|)` in Ela 0.9 were just syntax sugar over function application - not actual functions. In Ela 0.10 these are functions defined in standard Prelude. One can override them if needed, partially apply, etc. Priority and associativity of these operators haven't changed.

## Module references ##

In Ela 0.9 referencing a module like so:

```
open Foo
```

would create a public binding of a module instance to a name `Foo`. In Ela 0.10 this binding is private and is not included in an export list of a module.

## Concatenation ##

In Ela 0.9 code like so would result in run-time error:

```
let _ = "string" ++ 2
let _ = 2 ++ 3
```

In Ela 0.10 concatenation operator `(++)` by default formats all its arguments as strings and the code above would work and produce the same result as:

```
let _ = "string" ++ show 2
let _ = show 2 ++ show 3
```

For lists, tuples and records a behavior of `(++)` didn't change.

## Logical operators ##

In Ela 0.9 symbolic names `(&&)` and `(||)` were used for logical AND and logical OR operators. These operators were (and still are) special forms but not functions as soon as they are always executed in a lazy way while Ela uses a strict-by-default evaluation strategy.
In Ela 0.10 these operators are replaced by special keywords `and` and `or`. Symbols previously reserved by these operators can now be used in names of custom functions.

## Breaking standard library changes ##

Functions `rec`, `rec3` and `each` are moved from `Core` module to the new `Imperative` module. Indexing operator `(!!)` from standard prelude is renamed to `(:)`.

Module `Con` is now partially written in Ela (`Console.ela`) and renamed to `Console` because of technical reasons (Windows doesn't accept `Con` as a file name).