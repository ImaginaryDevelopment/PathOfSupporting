# PathOfSupporting
A .net helper library for tools working with Path of Exile or Path of Building
Call into it from any .net Language

## Major Sections:

### Skill gem info
  * mapped from PoE's official passive gem json [TreeParsing.fs](https://github.com/ImaginaryDevelopment/PathOfSupporting/blob/master/PoS/Parsing/TreeParsing.fs#L16)
### Item Parsing
  * parses out resists and total up resistances for an item - [ItemParsing.fs](https://github.com/ImaginaryDevelopment/PathOfSupporting/blob/master/PoS/Parsing/ItemParsing.fs)
  * planned: more item parsing/summations
### Tree Parsing
  * [TreeParsing.fs - PassiveJsParsing](https://github.com/ImaginaryDevelopment/PathOfSupporting/blob/master/PoS/Parsing/TreeParsing.fs#L92)
  * parses passive tree urls
  
### PoB Parsing
  * [TreeParsing.fs - PathOfBuildingParsing](https://github.com/ImaginaryDevelopment/PathOfSupporting/blob/master/PoS/Parsing/TreeParsing.fs#L208)
  * parses PoB links
  * fetches PoB links from a pastebin url
  
### Character fetching
  * [HtmlParsing.fs](https://github.com/ImaginaryDevelopment/PathOfSupporting/blob/master/PoS/Parsing/HtmlParsing.fs)
  * fetches character names from an account name (given that the account's privacy allows character browsing)
  

### Dps calculations - [Dps.fs](https://github.com/ImaginaryDevelopment/PathOfSupporting/blob/master/PoS/Dps.fs)
  * Weapon Dps Calculations - (the numbers match up with PoB) - Partially completed 
  
  

Installation 
 * NuGet - https://www.nuget.org/packages/PathOfSupporting/
 * Source - https://github.com/ImaginaryDevelopment/PathOfSupporting
 
  
 
  
 
  
