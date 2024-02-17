﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PLang.Interfaces;
using PLang.Models;
using PLang.Services.LlmService;
using PLang.Utils;
using PLang.Utils.Extractors;
using System.Text;
using static PLang.Services.LlmService.PLangLlmService;

namespace PLang.Services.OpenAi
{
    public class OpenAiService : ILlmService
	{
		OpenAI_API.OpenAIAPI api;
		private readonly ISettings settings;
		private readonly ILogger logger;
		private readonly LlmCaching llmCaching;
		private readonly PLangAppContext context;

		public IContentExtractor Extractor { get; set; }

		public OpenAiService(ISettings settings, ILogger logger, LlmCaching llmCaching, PLangAppContext context)
		{
			this.logger = logger;
			this.llmCaching = llmCaching;
			this.context = context;
			this.settings = settings;

			this.Extractor = new JsonExtractor();
		}


		public virtual async Task<T?> Query<T>(LlmRequest question)
		{
			return (T?)await Query(question, typeof(T));
		}
		public virtual async Task<object?> Query(LlmRequest question, Type responseType)
		{
			return await Query(question, responseType, 0);
		}
		public virtual async Task<object?> Query(LlmRequest question, Type responseType, int errorCount)
		{
			Extractor = ExtractorFactory.GetExtractor(question, responseType);

			var q = llmCaching.GetCachedQuestion(question);
			if (!question.Reload && question.caching && q != null)
			{
				try
				{
					JsonConvert.DeserializeObject(q.RawResponse);
					return Extractor.Extract(q.RawResponse, responseType);
				}
				catch { }
			}

			var httpClient = new HttpClient();
			var httpMethod = new HttpMethod("POST");
			var request = new HttpRequestMessage(httpMethod, "https://api.openai.com/v1/chat/completions");

			string bearer = settings.Get(this.GetType(), "Global_AIServiceKey", "", "Type in API key for OpenAI service");
			string data = $@"{{
		""model"":""{question.model}"",
		""temperature"":{question.temperature},
		""max_tokens"":{question.maxLength},
		""top_p"":{question.top_p},
		""frequency_penalty"":{question.frequencyPenalty},
		""presence_penalty"":{question.presencePenalty},
		""messages"":{JsonConvert.SerializeObject(question.promptMessage)}
			}}";
			request.Headers.UserAgent.ParseAdd("plang v0.1");
			request.Headers.Add("Authorization", $"Bearer {bearer}");
			request.Content = new StringContent(data, Encoding.GetEncoding("UTF-8"), "application/json");
			httpClient.Timeout = new TimeSpan(0, 5, 0);
			try
			{

				var response = await httpClient.SendAsync(request);

				string responseBody = await response.Content.ReadAsStringAsync();
				if (!response.IsSuccessStatusCode)
				{
					throw new Exception(responseBody);
				}

				var json = JsonConvert.DeserializeObject<dynamic>(responseBody);
				if (json == null || json.choices == null || json.choices.Count == 0)
				{
					throw new Exception("Could not parse OpenAI response: " + responseBody);
				}

				question.RawResponse = json.choices[0].message.content.ToString();

				var obj = Extractor.Extract(question.RawResponse, responseType);
				if (question.caching)
				{
					llmCaching.SetCachedQuestion(question);
				}
				return obj;
			}
			catch (Exception ex)
			{
				if (errorCount < 3)
				{
					string assitant = $@"
### error in your response ###
I could not deserialize your response. This is the error. Please try to fix it.
{ex.ToString()}
### error in your response ###
";
					question.promptMessage.Add(new LlmMessage("assistant", assitant));

					var qu = new LlmRequest(question.type, question.promptMessage, question.model, question.caching);
					return await Query(qu, responseType, ++errorCount);
				}

				throw;

			}
		}



	}
}
