using System.Collections.Generic;
using Interface;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System;
using System.Linq;

/*
    This transformer calculates an average value by storing information
    regarding the previous transformations. Thus it only needs to know the
    value of the resource that triggered the transformation.
*/

namespace AverageTransformerImplementation
{
    public class AccumulatingAverageTransformer : ITransformer
    {
        private const int outputResourceId = 10;
        public List<int> InputResourceIds =>
            new List<int>()
            {
                0,
                1
            };

        public List<int> OutputResourceIds =>
            new List<int>()
            {
                outputResourceId
            };

        public ScopeOfInterest ScopeOfInterest =>
            ScopeOfInterest.TriggeringResource;

        public string InitialState =>
            JsonConvert
            .SerializeObject(
                new InternalState()
                {
                    Average = 0,
                    Count = 0
                }
            );

        public Task<(string, Dictionary<int, double>)> Transform(
            string serializedState,
            Dictionary<int, double> measurements
        )
        {
            var state = JsonConvert
                .DeserializeObject<InternalState>(serializedState);

            var newData = measurements
                .Select(pair => pair.Value)
                .Aggregate(0.0, Add);

            var total = state.Average * state.Count + newData;
            var count = state.Count + measurements.Count();
            var average = total / count;

            var newState = new InternalState() {
                Average = average,
                Count = count
            };

            var serializedNewState = JsonConvert
                .SerializeObject(newState);

            var outputValues = new Dictionary<int, double> {
                {outputResourceId, average}
            };

            return Task.FromResult((serializedNewState, outputValues));
        }

        public static double Add(double a, double b) => a + b;
    }

    [Serializable]
    public struct InternalState
    {
        public double Average;
        public int Count;
    }
}