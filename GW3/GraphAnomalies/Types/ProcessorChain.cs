namespace GraphAnomalies.Types
{
    public struct ProcessorChain
    {
        public readonly Processor FirstProcessor;
        public readonly Processor LastProcessor;

        public ProcessorChain(Processor firstProcessor, Processor lastProcessor)
        {
            FirstProcessor = firstProcessor;
            LastProcessor = lastProcessor;
        }
    }
}