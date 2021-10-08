module ConstraintTests

open Xunit
open ConstraintProto
open Constraint


[<Fact>]
let ``Constraint Tests`` () =
   let smallMoney = (or' (matchRegex @"^\$\d+$") (oneOf ["0"])) &&& maxLength 5
   let sm = validate smallMoney

   //I'd be good to have a not in, 
   let pickyRange = max 10 &&& min 0 &&& oneOf [3; 4; 5; 11]
   let pr = validate pickyRange 
   let smResults = [(sm "$5"); (sm "5"); sm "10000000"; sm "0"] 
   let prResults = [pr 3; pr 6; pr 11]
   ()

