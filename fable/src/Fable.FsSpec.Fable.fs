module Fable.FsSpec.Fable

open Fable.Core

(************************************************)
(*     You should remove the next lines and     *)
(*    start writing your library in this file   *)
(************************************************)

[<Emit("$0 + $1")>]
let add (x: int) (y: int) = jsNative