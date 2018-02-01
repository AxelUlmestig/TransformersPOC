using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AverageTransformerImplementation;
using Interface;

/*
    This example uses two transformers that calculate average values of
    resources. One of them calculates it by looking at a snapshot of all the
    current values and one does it by aggregating values and then
    calculating.

    This uses a simplified model where all resources have a predefined id
    that the transformers know of. In reality we would use some sort of type
    id in the transformers which we would then have to translate to reference
    ids. Hopefully this is a fair simplification to get this proof of concept
    across.

    Another simplification that was done is that all resources that the
    transformers use have initial values. I never handle the case when a
    resource doesn't have a value.

    Here I also assume that all resource values are double values.

    The code written here is in no way optimized for performance and should
    under no circumstances be used in a system that has performance
    requirements.
*/

namespace Transformers
{
    class TransformerHandler
    {
        static void Main(string[] args)
        {
            var handler = new TransformerHandler(
                new List<ITransformer>()
                {
                    new AccumulatingAverageTransformer(),
                    new CurrentAverageTransformer()
                }
            );

            handler.MeasureValue(0, 2);

            Console.WriteLine("Resource values after transformations:");
            handler
                .ResourceValues
                .ToList()
                .ForEach(x => Console.WriteLine(x));

            Console.WriteLine("\nTransformer states after transformations:");
            handler
                .TransformerStates
                .ToList()
                .ForEach(x => Console.WriteLine(x));
        }

        private Dictionary<Guid, ITransformer> Transformers =
            new Dictionary<Guid, ITransformer>();

        public Dictionary<Guid, string> TransformerStates =
            new Dictionary<Guid, string>();

        public Dictionary<int, double> ResourceValues =
            new Dictionary<int, double>()
            {
                {0, 123},
                {1, 456},
                {2, 789}
            };

        public TransformerHandler(IEnumerable<ITransformer> transformers)
        {
            foreach(var transformer in transformers) 
            {
                var id = Guid.NewGuid();
                Transformers[id] = transformer;
                TransformerStates[id] = transformer.InitialState;
            }
        }

        public TransformerHandler MeasureValue(int resourceId, double value)
        {
            ResourceValues[resourceId] = value;

            Transformers
                .Where(IdAndTransformer => IdAndTransformer.Value.InputResourceIds.Contains(resourceId))
                .Select(IdAndTransformer => ApplyTransformer(
                    ResourceValues,
                    resourceId,
                    IdAndTransformer.Key,
                    TransformerStates[IdAndTransformer.Key],
                    IdAndTransformer.Value
                ))
                .ToList()
                .ForEach(async valuesAndState => {
                    var (newValues, newState) = await valuesAndState;
                    ResourceValues = MergeDictionaries(ResourceValues, newValues);
                    TransformerStates = MergeDictionaries(TransformerStates, newState);
                });

            return this;
        }

        public static async Task<(Dictionary<int, double>, Dictionary<Guid, string>)> ApplyTransformer(
            Dictionary<int, double> values,
            int triggeringResource,
            Guid transformerId,
            string state,
            ITransformer transformer
        )
        {
            var relevantResources = values
                .Where(GenerateResourceFilter(transformer, triggeringResource))
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            var (outputState, rawOutputValues) = await transformer.Transform(state, relevantResources);

            var outputValues = rawOutputValues
                .Where(IdAndValue => transformer.OutputResourceIds.Contains(IdAndValue.Key))
                .ToDictionary(IdAndValue => IdAndValue.Key, IdAndValue => IdAndValue.Value);

            var outputStateDictionary = new Dictionary<Guid, string>() {
                { transformerId, outputState }
            };

            return (outputValues, outputStateDictionary);
        }

        public static Func<KeyValuePair<int, double>, bool> GenerateResourceFilter(
            ITransformer transformer,
            int triggeringResource
        )
        {
            switch (transformer.ScopeOfInterest)
            {
                case ScopeOfInterest.TriggeringResource:
                    return idAndValue => idAndValue.Key == triggeringResource;
                case ScopeOfInterest.AllResources:
                default:
                    return _ => true;
            }
        }

        public static Dictionary<T, U> MergeDictionaries<T, U>(
            Dictionary<T, U> d1,
            Dictionary<T, U> d2
        )
        {
            var union = new Dictionary<T, U>();

            d1
                .ToList()
                .ForEach(keyValue => union[keyValue.Key] = keyValue.Value);

            d2
                .ToList()
                .ForEach(keyValue => union[keyValue.Key] = keyValue.Value);

            return union;
        }
    }
}