# PathOfSupporting
A .net helper library for tools working with Path of Exile or Path of Building
Call into it from any .net Language

[![Build status](https://ci.appveyor.com/api/projects/status/85y72cy90rs59wkv?svg=true)](https://ci.appveyor.com/project/ImaginaryDevelopment/pathofsupporting)

### Installation 
 * NuGet - https://www.nuget.org/packages/PathOfSupporting/
 * Source - https://github.com/ImaginaryDevelopment/PathOfSupporting
 
## Major Sections:

### Skill gem info
  * mapped from PoE's official passive tree page [TreeParsing.fs](https://github.com/ImaginaryDevelopment/PathOfSupporting/blob/master/PoS/Parsing/TreeParsing.fs#L16)
### Item Parsing
  * parses out resists and does summations on resistances for an item - [ItemParsing.fs](https://github.com/ImaginaryDevelopment/PathOfSupporting/blob/master/PoS/Parsing/ItemParsing.fs)
  * sample C# consumer - [ItemParsing.cs](https://github.com/ImaginaryDevelopment/PathOfSupporting/blob/master/SampleConsumer/Parsing/ItemParsing.cs)
  * planned: more item parsing/summations
### Tree Parsing
  * [TreeParsing.fs - PassiveJsParsing](https://github.com/ImaginaryDevelopment/PathOfSupporting/blob/master/PoS/Parsing/TreeParsing.fs#L92)
  * parses passive tree urls
  * sample C# consumer - [TreeParsing.cs](https://github.com/ImaginaryDevelopment/PathOfSupporting/blob/master/SampleConsumer/Parsing/TreeParsing.cs)
  
### PoB Parsing
  * [TreeParsing.fs - PathOfBuildingParsing](https://github.com/ImaginaryDevelopment/PathOfSupporting/blob/master/PoS/Parsing/TreeParsing.fs#L208)
  * parses PoB links
  * fetches PoB links from a pastebin url
  
### Character fetching
  * [HtmlParsing.fs](https://github.com/ImaginaryDevelopment/PathOfSupporting/blob/master/PoS/Parsing/HtmlParsing.fs)
  * fetches character names from an account name (given that the account's privacy allows character browsing)


### Dps calculations - [Dps.fs](https://github.com/ImaginaryDevelopment/PathOfSupporting/blob/master/PoS/Dps.fs)
  * Weapon Dps Calculations - (the numbers match up with PoB) - Partially completed 
  
  


 
  
 
  
 
  
