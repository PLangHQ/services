# Template for build .service


The example contains custom implementation of LLM service


LLM service is special if you want to use it to build code. It must be injected with the @ character
This is because it needs to parse the inject as soon as builder starts and replace the default llm service
	  
```plang
@llm=NewLlmService
	  
Start
- write out 'hello'
```

	  
This should cause the NewLlmService to be used to build the plang code.
	  
If the service is not meant for building code, you can inject it like any other
	  

```plang 
Start
- inject llm, 'NewLlmService'
- [llm] system:Hello
	 	user: How are your
	 	write to %response%
- write out %response%
```

	  
	  
To build the C# code, open terminal, navigate to folder of your .cs file, run command

```bash
dotnet build
```

This will build your C# code, find the .dll file in the bin folder, copy them to your project .services folder.