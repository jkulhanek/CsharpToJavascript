# CsharpToJavascript
Csharp lambda expression to javascript converter

This product is used to convert the popular Csharp lambda into javascript function. It is perfect for solutions with specific html field validations on either server or client side. It also suits projects with data binding. Or you can find another useful purpose for it.

It can convert expression to evaluate on the client side only and missing server object renders as json and pass it onto the client side or you can specify, that a specific object is on javascript side and you only want to access it. In this case you create a blank model representing client side and use it in your lambda.
You are also able to choose between whether you want the result to be a inline function or full named function object.
Current features:

* Accessing members
* Creating new instances of objects and arrays
* Serializing even complex types with members of type: string, char, DateTime, Timespan, Numbers, Arrays, Another complex types, or whatever supports ToString()
* Supported operators are +=,&,&&,&=,=,?:,/,/=,==,^,^=,>,>=,<,<=,!=,|,%,%=,*,*=,|=,||,^,^=,-,-=, array access, array length,++,--,basic type convert, method calling.

Expected features:
* Implement basic csharp System.* functions lime Math object and more.

