Here’s a cleaned-up and optimized version of the instructions:

---

# LLM Service Template

This template demonstrates a custom implementation of an LLM service.

### Steps to Implement

1. **Modify the `NewLlmService.cs` file:**

   Update the following properties in the file:
   ```csharp
   base.url = "https://localhost";
   base.settingKey = "NewLlmService";
   base.appId = ""; // set GUID here, go to https://guidgenerator.com/ for GUID
   ```

2. **Customize the `Query` Method:**

   Modify the `Query` method to fit your specific needs:
   ```csharp
   public override async Task<(object?, IError?)> Query(LlmRequest question, Type responseType, int errorCount)
   {
       // Your custom implementation here
   }
   ```

### Using the LLM Service for Code Generation

To use the service for code generation, inject it with the `@` character as shown below. This ensures the service is parsed and injected when the builder starts, replacing the default LLM service:

```plang
@llm=NewLlmService

Start
- write out 'hello'
```

This will use `NewLlmService` to build the `plang` code.

### Using the LLM Service for Other Purposes

If the service is not intended for code generation, you can inject it like this:

```plang
Start
- inject llm, 'NewLlmService'
- [llm] system:Hello
  user: How are you?
  write to %response%
- write out %response%
```

### Building the C# Code

1. Open a terminal and navigate to the folder containing your `.cs` file.
2. Run the following command to build the project:

   ```bash
   dotnet build
   ```

3. After building, find the `.dll` file in the `bin` folder and copy it to your project’s `.services` folder.

---

This version is streamlined for clarity and precision, with clear steps and code snippets to guide you through the implementation and usage of the LLM service.