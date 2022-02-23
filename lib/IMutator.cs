namespace bot
{
    /// <summary>
    /// Иногда Score можно посчитать быстрее, чем сформировать мутированное состояние.
    /// Тогда эффективнее разделять дешевую в создании IMutation, чтобы не создавать мутированное состояние в случае,
    /// если мутация будет забракована из-за низкого Score.
    /// </summary>
    public interface IMutator<in TProblem, TSolution>
    {
        IMutation<TSolution> Mutate(TProblem problem, TSolution parentSolution);
    }

    public interface IMutation<out TResult>
    {
        double Score { get; }
        TResult GetResult();
    }
}