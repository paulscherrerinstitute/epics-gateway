using GraphAnomalies.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphAnomalies
{
    public class ProcessorChainBuilder
    {
        public readonly List<Processor> Processors = new List<Processor>();

        private ProcessorChainBuilder()
        {
        }

        public ProcessorChainBuilder Chain(Processor processor)
        {
            Processors.Add(processor);
            return this;
        }

        public ProcessorChain Build()
        {
            var firstProcessor = Processors.First();
            var lastProcessor = firstProcessor;
            for (var i = 1; i < Processors.Count; i++)
            {
                var currentProcessor = Processors[i];
                // Make sure that we dont register the event handlers twice
                lastProcessor.ValueProcessed -= currentProcessor.Update;
                lastProcessor.ValueProcessed += currentProcessor.Update;
                lastProcessor = currentProcessor;
            }
            return new ProcessorChain(firstProcessor, lastProcessor);
        }

        public static ProcessorChainBuilder From(Processor first)
        {
            return new ProcessorChainBuilder().Chain(first ?? throw new ArgumentNullException(nameof(first)));
        }
    }
}