using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using TaskData.WorkTasks;
using TaskerAgent.Domain;
using TaskerAgent.Domain.RepetitiveTasks;

namespace TaskerAgent.Infra.Persistence.Context.Serialization
{
    internal class RepetitiveTaskConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IWorkTask);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                JContainer jContainer = JObject.Load(reader);
                objectType = (jContainer["Frequency"].ToObject<Frequency>()) switch
                {
                    Frequency.Daily => typeof(DailyRepetitiveMeasureableTask),
                    Frequency.Weekly => typeof(WeeklyRepetitiveMeasureableTask),
                    Frequency.Monthly => typeof(MonthlyRepetitiveMeasureableTask),
                    _ => typeof(DailyRepetitiveMeasureableTask),
                };

                reader = jContainer.CreateReader();
                return serializer.Deserialize(reader, objectType);
            }

            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value, typeof(DailyRepetitiveMeasureableTask));
        }
    }
}