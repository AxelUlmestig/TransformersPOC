using System.Collections.Generic;
using System.Threading.Tasks;

/*
    This version of the transformer interface lets the implementers specify
    the scope of resources that they are interested in when they write the
    transformer instead of on every transformation.

    This limits what you can do with the transformer but it also simplifies
    the interface. The transformer now receives all the resources that it
    asks for with the combination of the InputResourceIds and ScopeOfInterest
    every time a new transformation should be done.
    
    The Input/Ouput resources as well as the ScopeOfInterest are intended to
    be read once and then be considered immutable.

    In reality the Dictionary would need to have some other type signature to
    allow for multiple resources to have the same type id. Maybe something
    like Dictionary<int, List<Measurement>> ?
*/

namespace Interface
{
    public interface ITransformer
    {
        List<int> InputResourceIds { get; }
        List<int> OutputResourceIds { get; }
        ScopeOfInterest ScopeOfInterest { get; }
        string InitialState { get; }
        Task<(string, Dictionary<int, double>)> Transform(
            string state,
            Dictionary<int, double> measurements
        );
    }

    public enum ScopeOfInterest
    {
        TriggeringResource,
        AllResources
    }
}