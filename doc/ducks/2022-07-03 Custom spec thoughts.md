

## Original thought thread

Having custom meta simply as obj makes more and more sense.
No two customs of a tree need share a meta type. There is no generic consistency to enforce. It is an 'any' type, and on .net obj is any.

Consumers can still pattern match on meta type of they need to access it or use it to differentiate custom constraints. Heck, they could even use it to store objects that define behaviors (like explaining, validation, etc)

I could replace the label with just a meta type, but that makes explanation formatting less natural 

This has none of the smells from my chat library misadventure. Meta is a contained grab bag as far as the library is concerned. I don't care what they put there. I just pass it along for them to work with it later. 


For good measure. Strings and tags or other dictionaries are not sufficient nor are they more beneficial than just any object. I don't need to know anything about the structure of their meta. No sorting or filtering or similar operations


I suppose I may need equality on meta for tree comparison