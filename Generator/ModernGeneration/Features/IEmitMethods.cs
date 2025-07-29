namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Moduł może generować metody.
    /// </summary>
    public interface IEmitMethods : IFeatureModule
    {
        void EmitMethods(Context.GenerationContext ctx);
    }
}
