using composite.unity.Common;
using composite.unity.Core;
using System.Threading.Tasks;
using UnityEngine;

namespace composite.unity.Example
{
    public class ExampleCompositeRoot : CompositeRootBase
    {
        [SerializeField] private int somePayload = 1337;

        private FooService fooService;

        public override void InstallBindings()
        {       
            BindAsLocal<IFooService>(fooService);
        }
    }

    //---------------------------SAMPLES--------------------------------//

    // Сервис который создает другой композит рут, который стоит по иерархии выше.
    public interface IBooService { }

    public interface IFooService { }

    public class FooService : IFooService,
        ICommandListener<ExampleServiceStartedCommand>,
        ICommandListener<ExampleServiceWithPayloadStartedCommand>
    {
        private readonly IBooService booService;

        public FooService(IBooService booService)
        {
            this.booService = booService;
        }

        public void OnEnable()
        {
            // подписались  
        }

        public void OnDisable()
        {
            // отписались
        }

        public void Initialize()
        {
            // проинициализировали что нужно
        }

        public void Tick()
        {
            // TIIICKKK!
        }

        public void ReactCommand(ExampleServiceStartedCommand command)
        {
            // какая то логика
        }

        public void ReactCommand(ExampleServiceWithPayloadStartedCommand command)
        {
            // какая то логика но с данными, пример GameFinishedCommand c данными о победе коинсах итд,
            // это может слушать аналитика, сохранения и тому подобные сервисы.
            // command.Payload1
        }
    }

    public struct ExampleServiceStartedCommand : ICommand { }

    public struct ExampleServiceWithPayloadStartedCommand : ICommand
    {
        public int Payload;
        public int Payload2;
        public int Payload3;

        public ExampleServiceWithPayloadStartedCommand(int payload, int payload2, int payload3)
        {
            this.Payload = payload;
            this.Payload2 = payload2;
            this.Payload3 = payload3;
        }
    }
}