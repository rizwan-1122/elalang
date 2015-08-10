# Code samples in Ela #


### Factorial ###

```
fact = fact' 1L 
       where fact' acc 0 = acc
             fact' acc n = fact' (n * acc) (n - 1)

fact 20 //2432902008176640000
```


### Fibonacci ###

```
fib = fib' 0 1
      where fib' a b 0 = a
            fib' a b n = fib' b (a + b) (n - 1)

fib 12 //144
```


### Reverse a list ###

```
foldl f z (x::xs) = foldl f (f z x) xs
foldl _ z []      = z

reverse = foldl (flip (::)) []

reverse' [0..5] //[5,4,3,2,1,0]
```


### Intersperse ###

```
intersperse e (x::[]) = x :: intersperse e []
intersperse e (x::xs) = x :: e :: intersperse e xs
intersperse _ []      = []
    
intersperse "," "abcdef" //"a,b,c,d,e,f"
intersperse 0 [1,2,3] //[1,0,2,0,3]
```


### Sieve of Eratosthenes ###

```
open list

primes xs = sieve xs
            where sieve (p::xs) = p :: (& sieve [& x \\ x <- xs | x % p > 0])

primes [2..] |> take 10 //[2,3,5,7,11,13,17,19,23,29]
```


### Transpose rows and columns in a list ###

```
transpose [] = []
transpose ([]::xs)      = transpose xs
transpose ((x::xs)::ys) = (x :: [h \\ (h::_) <- ys]) :: transpose (xs :: [t \\ (_::t) <- ys])

transpose [[1,2,3],[4,5,6]] //[[1,4],[2,5],[3,6]]
```


### Subsequences in a list ###

```
open list
subs [] = []
subs (x::xs) = [x] :: fold f (subs xs)
               where f ys r = ys :: (x :: ys) :: r                         

subs [1,2,3] //[[1],[2],[1,2],[3],[1,3],[2,3],[1,2,3]]
```


### Permutations ###

```
open list
permutations []  = []
permutations xs0 = xs0 :: perms xs0 []
               where perms []    _   = []
                     perms (t::ts) is' = foldr interleave (perms ts (t::is')) (permutations is')
                            where interleave xs r         = let (_,zs) = interleave' id xs r in zs
                                  interleave' _ [] r      = (ts, r)
                                  interleave' f (y::ys) r = let (us,zs) = interleave' (f << (y::)) ys r
                                                            in  (y::us, f (t::y::us) :: zs)

permutations "abc"   //["abc","bac","cba","bca","cab","acb"]
permutations [1,2,3] //[[1,2,3],[2,1,3],[3,2,1],[2,3,1],[3,1,2],[1,3,2]]
```


### ABIN theorem prover ###

```
open list

is_true x            = is_axiom x || is_theorem x
is_expr x            = is_b_expr x || is_a_expr x || is_n_expr x
is_axiom x           = is_n_expr x && is_b_expr (tail x)
is_b_expr ['B']      = true
is_b_expr ('B'::xs)  = all (=='I') xs
is_b_expr  _         = false 
is_n_expr ('N'::xs)  = is_expr xs
is_n_expr  _         = false               
split _ []           = []
split p (x::xs)      = split' [x] xs
  where split' _  [] = false
        split' l1 (x::xs)@l2 
          | p l1 l2 = l1 
          | else    = split' (l1 ++ [x]) xs
have_two_exprs xs ys = is_expr xs && is_expr ys
check_two_exprs lst  = split have_two_exprs lst is List
is_a_expr ('A'::xs)  = check_two_exprs xs
is_a_expr  _         = false               
is_theorem x         = is_n_expr x && is_a_expr (tail x) && has_subtheorem x
  where has_subtheorem lst = is_true ('N'::first)
          where first = split have_two_exprs (tail (tail x))

xs = ["NBI", "NBIBI", "NABIBI", "NANBBI", "ABB", "NABIBIBI", "NAABABBBI"]
map is_true xs //[True,False,True,False,False,False,True]
```

ABIN theorem is taken from: W. Robinson, Computers, minds and robots, Temple University Press, 1992.

Implementation in [Pure](http://code.google.com/p/pure-lang): [here](http://code.google.com/p/pure-lang/wiki/PurePrimer2#Complete_code_for_ABIN)


### Van der Corput sequence ###

```
open math list
 
vdc bs n = vdc' 0.0 1.0 n
          where vdc' v d n | n > 0 = vdc' v' d' n'
                           | else  = v
                           where d' = d * bs
                                 rem = n % bs
                                 n' = truncate (n / bs)
                                 v' = v + rem / d'

take 5 <| map' (vdc 2.0) [1..] //[0.5,0.25,0.75,0.125,0.625]
```

More code samples as [RosettaCode](http://rosettacode.org/wiki/Ela).