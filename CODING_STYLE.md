# Coding Style
In this article it's described the general coding style rules and guidelines to be followed when contributing to the COTLMP mod project with code.
Specifically a good code is deemed as such if it follows the file structure, indentation and other miscellaneous guidelines below.

## File Structure
Files, usually created as C# files, must come up with a file header such like this:
```
/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     I have such a cool purpose
 * COPYRIGHT:	Copyright 2025 Bob The Goober <bobgoober@mail.com>
 */
```

You must describe what's the purpose of your file and what it implements in `PURPOSE` field, and add yourself to `COPYRIGHT` field if you have made substantial changes to a code file or you created one.
Only a maximum of 5 people can be added in the copyright list.

At the end of the file you must add `/* EOF */` with an empty newline after it.

## Indentation rules
1. You must indent the code with 4 spaces, don't use tabs!
2. Do not add a space or tab at the end of any line.

## Commenting
Generally using `/* */` is the preferred option when commenting code but `//` (aka single line comments) is also allowed.
However for multi-line comments it's recommended to use the first option when possible!

## Documentation
Documenting code is an important part when contributing to the mod as it helps other contributors and future potential contributors (or collaborators) to understand what it tries to do and why.
Typically a documentation header looks like this:
```
/*
 * @brief
 * It does cool stuff and also does cool stuff there too.
 *
 * @param[in,out] Param1
 * Param1 is beautiful.
 *
 * @return
 * Returns something cool.
 *
 * @remarks
 * But also it does super stuff too!
 */
```
`@brief` denotes the description of something that is being documented, `@param[in,out]` to denote that the following part is a parameter and `@return` describes what stuff does it return.
Typically such a header like this is used to document methods inside a class.
To document other parts of the code like namespaces, structures and whatnot you must include different sections in the documentation header. The allowed sections are:

`@field` - Used to describe a member field or an enumeration field. Use this if you are documenting an enumeration or a data structure.
`@class` - Used to describe a class. Use this if you are documenting a class of which a namespace implements.
`@param[in,out]` - Used to describe a parameter. `in` is an annotation which describes the following parameter takes an argument from the caller, whilst `out` means some data is returned to the caller to that parameter. The caller is responsible to host stack space for that parameter. If a parameter does the following two things, like typically taking an argument and initializing it with stuff, then you must use `in,out` within the brackets.
`@return` - Used to describe what kind of data is returned after a method finishes execution. This is EXCLUSIVELY USED when documenting methods! If a method doesn't return anything (aka a method is `void`) then you mustn't include this section in the documentation header.
`@remarks` - Used to describe some important remarks. Usually you want to describe some considerations or notes about the construct you are documenting. Don't add this section if you have no remarks to describe about!

## File Sections
When creating a file you must categorize the parts of your file into sections:
```
/* IMPORTS ********************************************************************/
```
This is used to to denote library imports that a file uses them.
```
/* CLASSES & CODE *************************************************************/
```
This is used to denote actual code and classes that a file implements.
```
/* TRANSLATION ****************************************************************/
```
This is used to denote the following part of the file is a translation.

You can define your own section which describes a totally different part of your file. Make sure that the asterisks row length match with the rest of other section headers!
