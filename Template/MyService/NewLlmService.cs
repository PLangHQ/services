using Microsoft.Extensions.Logging;
using PLang.Errors;
using PLang.Interfaces;
using PLang.Models;
using PLang.Services.LlmService;
using PLang.Services.OpenAi;

namespace MyLlmService
{
	/*
	 * LLM service is special if you want to use it to build code. It must be injected with the @ character
	 * This is because it needs to parse the inject as soon as builder starts and replace the default llm service
	 * 
	 * --- Start.goal ---
	 * @llm=NewLlmService
	 * 
	 * Start
	 * - write out 'hello'
	 * --- Start.goal ---
	 * 
	 * This should cause the NewLlmService to be used.
	 * 
	 * If the service is not meant for building code, you can inject it like any other
	 * 
	 * --- Start.goal ---	 
	 * 
	 * Start
	 * - inject llm, 'NewLlmService'
	 * - [llm] system:Hello
	 *			user: How are your
	 *			write to %response%
	 * - write out %response%
	 * 
	 * --- Start.goal ---
	 * 
	 * 
	 * To build the C# code, open terminal, navigate to folder of your .cs file, run command
	 * > dotnet build
	 */

	public class NewLlmService : OpenAiService
	{
		public NewLlmService(ISettings settings, ILogger logger, LlmCaching llmCaching, PLangAppContext context) : base(settings, logger, llmCaching, context)
		{
			base.url = "https://localhost";
			base.settingKey = "NewLlmService";
			base.appId = ""; // set random GUID here, go to https://guidgenerator.com/ for GUID

			if (string.IsNullOrEmpty(base.appId))
			{
				throw new Exception("Set appId to enable caching between apps that you build");
			}
		}

		// since we want to modify the question we need to overwrite all Query methods

		public override async Task<(T?, IError?)> Query<T>(LlmRequest question) where T : class
		{
			var result = await this.Query(question, typeof(T));
			var item = (T?)result.Item1;

			return ((T?)result.Item1, result.Item2);
		}
		public override async Task<(object?, IError?)> Query(LlmRequest question, Type responseType)
		{
			return await this.Query(question, responseType, 0);
		}
		public override async Task<(object?, IError?)> Query(LlmRequest question, Type responseType, int errorCount)
		{
			string model = "defaultModelName"; //change default model name
			var modelArg = Environment.GetCommandLineArgs().FirstOrDefault(p => p.StartsWith("--model="));
			if (modelArg != null) {
				model = modelArg.Substring(modelArg.LastIndexOf('=') + 1);    
            }

			// some validation if model is valid should be done
			question.model = model;

			/*
			 * plang uses the assistant role that OpenAI supports, when llm doesnt support it
			 * you need to adjust question.promptMessage to move assisant into system role.
			 */
			var assistantMessages = question.promptMessage.Where(p => p.Role == "assistant").ToList();
			assistantMessages.ForEach(p =>
			{
				p.Role = "system";
			});

			
			return await base.Query(question, responseType, errorCount);
		}
	}
}
