using System;
using System.Threading;

namespace Threads3
{
    class StartClass
    {
        static Thread th0, th1;
        static CommonData commonData;
        static long result = 0;

        static void Main(string[] args)
        {
            StartClass.commonData = new CommonData(0);
            // Конструкторы классов Worker и Inspector несут дополнительную нагрузку.
            // Они обеспечивают необходимыми значениями методы, выполняемые во вторичных потоках.

            // До начала выполнения потока вся необходимая информация доступна методу.
            Worker work = new Worker(ref commonData);

            // На инспектора возложена дополнительная обязанность вызова функции-терминатора.
            // Для этого используется специально определяемый и настраиваемый делегат.
            Inspector insp = new Inspector(ref commonData, 50000, new CallBackFromStartClass(StartClass.StopMain));

            // Стартовые функции потоков должны соответствовать сигнатуре
            // класса делегата ThreadStart. Поэтому они не имеют параметров.
            ThreadStart t0, t1;
            t0 = new ThreadStart(work.startWorker);
            t1 = new ThreadStart(insp.startInspector);

            // Созданы вторичные потоки.
            StartClass.th0 = new Thread(t0);
            StartClass.th1 = new Thread(t1);

            // Запущены вторичные потоки.
            StartClass.th0.Start();
            StartClass.th1.Start();

            // Еще раз о методе Join(): Выполнение главного потока приостановлено.
            StartClass.th0.Join();
            StartClass.th1.Join();

            // Поэтому последнее слово остается за главным потоком приложения.
            Console.WriteLine($"Main(): All stoped at {result}. Bye.");
        }

        // Функция - член класса StartClass выполняется во ВТОРИЧНОМ потоке!
        public static void StopMain(long key)
        {
            Console.WriteLine($"StopMain: All stoped at {key}...");
            // Остановка рабочих потоков. Ее выполняет функция - член класса StartClass. 
            // Этой функции в силу своего определения известно ВСЕ о вторичных потоках.
            // Но выполняется она в ЧУЖОМ (вторичном) потоке. Поэтому:
            // 1) нужно предпринять особые дополнительные усилия для того, чтобы
            // результат работы потоков оказался доступен в главном потоке. 
            /*StartClass.*/
            result = key;
            // 2) очень важна последовательность остановки потоков,
            StartClass.th0.Abort();
            StartClass.th1.Abort();

            // Этот оператор не выполняется! Поток, в котором выполняется метод - член класса StartClass StopMain(), остановлен.
            Console.WriteLine("StopMain(): bye.");
        }
    }

    public delegate void CallBackFromStartClass(long param);
    // Общие данные - предмет и основа взаимодействия двух потоков.
    class CommonData
    {
        public long lVal;
        public CommonData(long key)
        {
            lVal = key;
        }
    }
    
    // Классы Worker и Inspector: взаимодействующие потоки
    class Worker
    {
        CommonData cd;
        public Worker(ref CommonData rCDKey)
        {
            cd = rCDKey;
        }

        public void startWorker()
        {
            DoIt(ref cd);
        }

        // Тело рабочей функции...
        public void DoIt(ref CommonData cData)
        {//====================================
            for (; ; )
            {
                cData.lVal++; // Изменили значение...
                Console.Write("{0,25}\r", cData.lVal); // Сообщили о результатах.
            }
        }//====================================
    }

    class Inspector
    {
        long stopVal;
        CommonData cd;
        CallBackFromStartClass callBack;

        // Конструктор... Подготовка делегата для запуска CallBack-метода. 
        public Inspector(ref CommonData rCDKey, long key, CallBackFromStartClass cbKey)
        {
            stopVal = key;
            cd = rCDKey;
            callBack = cbKey;
        }

        public void startInspector()
        {
            measureIt(ref cd);
        }


        // Тело рабочей функции...
        public void measureIt(ref CommonData cData)
        {//====================================
            for (; ; )
            {
                if (cData.lVal < stopVal)
                {
                    Thread.Sleep(500);
                    Console.Write("{0,55}\r", cData.lVal);
                }
                else
                    callBack(cData.lVal);
            }
        }//====================================
    }
}
