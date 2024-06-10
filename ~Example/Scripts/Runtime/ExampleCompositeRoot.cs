using composite.unity.Common;
using composite.unity.Core;
using System.Threading.Tasks;
using UnityEngine;

namespace composite.unity.Example
{
    public class ExampleCompositeRoot : CompositeRootBase, ITickable, IFixedTickable, ILateTickable
    {
        // демонстрация, что можно добавлять в поля всякие конфиги данные и тому подобное.
        [SerializeField] private int somePayload = 1337;

        private FooService fooService;

        //---------------------------INITIALIZE--------------------------------//

        // Методы расставлены в порядок вызовов, главный класс хендлера обрабатывает каждый,
        // Пример: сначала у всех InstallBindings, потом у всех PreInitialize и так далее.
        // Порядок инициализации важен, поэтому правильно расставляйте порядок инициализации.

        // Почему все асинхронно? Зачастую мы с сервера подкачиваем нужные конфиги итд
        // И нам нужно дожидаться некоторых инициализаций, каких не нужно не дожидаемся.

        // биндинги зависимостей происходят здесь, а также получение зависимостей от других классов.
        // пример как в том же Zenject -> InstallBindings.
        public override void InstallBindings()
        {
            // получение зависимости от композита который был выше по иерархии инициализации

            // получение сервиса, чей скоп глобальный(подробнее ниже)
            var booServiceIfGlobal = GetGlobal<IBooService>();
            // получение сервиса, чей скоп локальный(подробнее ниже)
            var booServiceIfLocal = GetLocal<IBooService>();

            fooService = new FooService(booServiceIfLocal);

            // биндинги:
            // локальный биндинг этот скоуп распространяется только на сцену, после завершения сцены он будет почищен;
            BindAsLocal<IFooService>(fooService);
            // глобальный биндинг этот скоуп распространяется на весь проект он жив пока жив проект
            BindAsGlobal<IFooService>(fooService);
            
            // можно создавать и таким образом они также будут сразу получать зависимости автоматически.
            CreateAndBindAsLocal<IFooService, FooService>();

            // Локальный евентбас (лучше не злоупотреблять)
            // Работает на скопе сцены
            // Если класс монобех и он на сцене он автоматически сам добавляется сюда не нужно писать это:
            LocalCommander.AddListener(fooService);
            // Так же мы можем биндить лисенера сразу при биндинге в скоуп, указав вторым аргументом true
            BindAsLocal<IFooService>(fooService, true);
            // Тут мы регаем лисенера
            // Также LocalCommandHandler можно получать как синглтон по всему проекту
            LocalCommandHandler.Instance.SendCommand<ExampleServiceStartedCommand>();
            LocalCommandHandler.Instance.SendCommand(new ExampleServiceWithPayloadStartedCommand
            {
                Payload = 1,
                Payload2 = 2,
                Payload3 = 3,
            });
            // команда шлется всем подписчикам
        }

        // выполняется перед инициализацией, здесь можно делать какие-нибудь подписки у сервисов.
        public async override Task PreInitialize()
        {
            fooService.OnEnable();
        }

        // инициализация тут уже нициализируем нужны нам объекты.
        public async override Task Initialize()
        {
            fooService.Initialize();
        }

        // этот код запускается когда все инициализировалось, пример запуск игры, туториала итд, конечная точка когда все готово.
        public async override Task Run()
        {

        }

        // сюда все отписки, этот код запускается когда сцена закончилась.
        public async override void OnBeforeDestroyed()
        {
            fooService.OnDisable();
        }

        //---------------------------UPDATE--------------------------------//

        // Мы также можем апдейтить наши композиты и их сервисы, наследуясь от нужного нам интерфейса цикла
        // Таким образом мы достигаем 1 апдейт на всю игру, что важно!

        public void Tick(float deltaTime)
        {
            fooService.Tick();
        }

        public void FixedTick(float deltaTime)
        {

        }

        public void LateTick(float deltaTime)
        {

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