namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Moduł może kontrybuować do konstruktora.
    /// </summary>
    public interface IEmitConstructor : IFeatureModule
    {
        void ContributeConstructor(Context.GenerationContext ctx);
    }
}
