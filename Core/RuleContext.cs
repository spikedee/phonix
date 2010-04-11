using System.Collections.Generic;

namespace Phonix
{
    public class RuleContext
    {
        private Dictionary<Feature, FeatureValue> _variableFeatures;
        private Dictionary<NodeFeature, IEnumerable<FeatureValue>> _variableNodes;

        public Dictionary<Feature, FeatureValue> VariableFeatures
        {
            get
            {
                if (_variableFeatures == null)
                {
                    _variableFeatures = new Dictionary<Feature, FeatureValue>();
                }
                return _variableFeatures;
            }
        }

        public Dictionary<NodeFeature, IEnumerable<FeatureValue>> VariableNodes
        {
            get
            {
                if (_variableNodes == null)
                {
                    _variableNodes = new Dictionary<NodeFeature, IEnumerable<FeatureValue>>();
                }
                return _variableNodes;
            }
        }
    }
}
