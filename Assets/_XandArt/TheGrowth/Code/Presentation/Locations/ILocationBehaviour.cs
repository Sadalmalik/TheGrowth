namespace TheGrowth.Presentation.Locations
{
    /// <summary>
    /// Компонент сцены локации, которому нужны зависимости. <see cref="LocationRoot"/> находит такие компоненты под
    /// собой, включая неактивные объекты, заполняет поля <c>[Inject]</c> из контейнера локации и вызывает
    /// <see cref="OnLocationInit"/> в порядке иерархии; <see cref="OnLocationDispose"/> — в обратном порядке, после
    /// отмены токена локации и до выгрузки сцены. <c>Awake</c> / <c>OnEnable</c> не должны зависеть от инжекта:
    /// у неактивного объекта они вызываются позже или не вызываются.
    /// </summary>
    public interface ILocationBehaviour
    {
        void OnLocationInit();

        void OnLocationDispose();
    }
}
