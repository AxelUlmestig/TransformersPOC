using System.Collections.Generic;
using Interface;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System;
using System.Linq;

/*
    This transformer calculates the average value of a number of resources by
    looking at a snapshot of their values every time the transformer is
    triggered. It does this by requesting ScopeOfInterest.AllResources.
*/

namespace AverageTransformerImplementation
{
    public class CurrentAverageTransformer : ITransformer
    {
        private const int outputResourceId = 11;
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
            ScopeOfInterest.AllResources;

        public string InitialState => "";

        public Task<(string, Dictionary<int, double>)> Transform(
            string serializedState,
            Dictionary<int, double> measurements
        )
        {
            var total = measurements
                .Select(pair => pair.Value)
                .Aggregate(0.0, Add);

            var average = total / measurements.Count();

            var outputValues = new Dictionary<int, double> {
                {outputResourceId, average}
            };

            return Task.FromResult((serializedState, outputValues));
        }

        public static double Add(double a, double b) => a + b;
    }
}