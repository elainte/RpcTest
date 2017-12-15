using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RpcTest
{
    public class CodeTimerAdvance
    {
        #region> 构造函数
        /// <summary>
        /// 默认构造函数，初始化GC代数为{0,0,0}
        /// </summary>
        public CodeTimerAdvance()
        {
            Gen = new[] { 0, 0, 0 };
        }
        #endregion

        #region> 共有属性
        /// <summary>次数</summary>
        public int Times { get; set; }
        /// <summary>迭代方法，如不指定，则使用Time(int index)</summary>
        public Action<int> Action { get; set; }
        /// <summary>是否显示控制台进度</summary>
        public bool ShowProgress { get; set; }
        /// <summary>进度</summary>
        public int Index { get; set; }
        /// <summary>CPU周期</summary>
        public ulong CpuCycles { get; set; }
        /// <summary>线程时间，单位是100ns，除以10000转为ms</summary>
        public long ThreadTime { get; set; }
        /// <summary>GC代数</summary>
        public int[] Gen { get; set; }
        /// <summary>执行时间</summary>
        public TimeSpan Elapsed { get; set; }
        #endregion

        #region>[静态]公有方法 > 快速计时

        /// <summary>
        /// 快速计时并且返回当前计时实例
        /// </summary>
        /// <param name="times"></param>
        /// <param name="action"></param>
        /// <param name="threadCount"></param>
        /// <returns></returns>
        public static CodeTimerAdvance Time(Int32 times, Action<Int32> action, int threadCount = 1)
        {
            CodeTimerAdvance timer = new CodeTimerAdvance
            {
                Times = times,
                Action = action
            };

            timer.TimeOne();   //不要预热的话就注释
            timer.Time(threadCount);
            return timer;
        }
        #endregion

        #region>[静态]公有方法 > 计时，并用控制台输出行

        /// <summary>
        /// 计时，并用控制台输出行
        /// </summary>
        /// <param name="title"></param>
        /// <param name="times"></param>
        /// <param name="action"></param>
        /// <param name="threadCount"></param>
        public static void TimeByConsole(String title, Int32 times, Action<Int32> action, int threadCount = 1)
        {
            ConsoleColor currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("{0,16}：\r\n", title);

            CodeTimerAdvance timer = new CodeTimerAdvance
            {
                Times = times,
                Action = action,
                ShowProgress = true
            };


            Console.ForegroundColor = ConsoleColor.Yellow;

            //timer.TimeOne();   //不要预热的话就注释
            timer.Time(threadCount);

            Console.WriteLine(timer.ToString());

            Console.ForegroundColor = currentForeColor;
        }
        #endregion

        #region>[虚拟]共有方法 > 计时核心方法，处理进程和线程优先级
        /// <summary>
        /// 计时核心方法，处理进程和线程优先级
        /// </summary>
        public virtual void Time(int threadCount)
        {
            if (Times <= 0) throw new Exception("非法迭代次数！");

            // 设定进程、线程优先级，并在完成时还原
            ProcessPriorityClass pp = Process.GetCurrentProcess().PriorityClass;
            ThreadPriority tp = Thread.CurrentThread.Priority;
            try
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                StartProgress();

                RealExcute(threadCount);
            }
            finally
            {
                StopProgress();

                Thread.CurrentThread.Priority = tp;
                Process.GetCurrentProcess().PriorityClass = pp;
            }
        }
        #endregion

        #region>[虚拟]保护方法 > 真正执行计时的方法
        /// <summary>
        /// 真正执行计时的方法
        /// </summary>
        protected virtual void RealExcute(int threadcount)
        {
            if (Times <= 0) throw new Exception("非法迭代次数！");

            // 统计GC代数
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            int[] gen = new Int32[GC.MaxGeneration + 1];
            for (Int32 i = 0; i <= GC.MaxGeneration; i++)
            {
                gen[i] = GC.CollectionCount(i);
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();
            ulong cpuCycles = GetCycleCount();
            long threadTime = GetCurrentThreadTimes();

            // 如果未指定迭代方法，则使用内部的Time
            Action<Int32> action = Action;
            if (action == null)
            {
                action = Time;
                // 初始化
                BeforeExcute();
            }

            DoActionLoop(action, threadcount);

            if (Action == null)
            {
                // 结束
                EndExcute();
            }

            CpuCycles = GetCycleCount() - cpuCycles;
            ThreadTime = GetCurrentThreadTimes() - threadTime;

            watch.Stop();
            Elapsed = watch.Elapsed;

            // 统计GC代数
            List<Int32> list = new List<Int32>();
            for (Int32 i = 0; i <= GC.MaxGeneration; i++)
            {
                int count = GC.CollectionCount(i) - gen[i];
                list.Add(count);
            }
            Gen = list.ToArray();
        }
        #endregion

        private void DoActionLoop(Action<Int32> action, int threadcount)
        {
            if (threadcount > 1)
            {
                List<Action> actions = new List<Action>();

                for (int a = 0; a < Times; a++)
                {
                    var index = a;
                    actions.Add(() =>
                    {
                        action(index);
                    });

                    if ((a + 1) % threadcount == 0)
                    {
                        Parallel.Invoke(actions.ToArray());
                        actions.Clear();
                    }
                }
            }
            else
            {
                for (Int32 i = 0; i < Times; i++)
                {
                    Index = i;

                    action(i);
                }
            }
        }

        #region>[虚拟]共有方法 > 执行一次迭代，可用作于预热所有方法
        /// <summary>
        /// 执行一次迭代，可用作于预热所有方法
        /// </summary>
        public void TimeOne()
        {
            Int32 n = Times;

            try
            {
                Times = 1;
                Time(1);
            }
            finally { Times = n; }
        }
        #endregion

        #region>[虚拟]共有方法 >  迭代前执行，计算时间
        /// <summary>
        /// 迭代前执行，计算时间
        /// </summary>
        public virtual void BeforeExcute() { }
        #endregion

        #region>[虚拟]共有方法 > 迭代后执行，计算时间
        /// <summary>
        /// 迭代后执行，计算时间
        /// </summary>
        public virtual void EndExcute() { }
        #endregion

        #region> 进度
        Thread _thread;
        void StartProgress()
        {
            if (!ShowProgress) return;

            // 使用低优先级线程显示进度
            _thread = new Thread(Progress)
            {
                IsBackground = true,
                //Priority = ThreadPriority.BelowNormal
            };
            _thread.Start();
        }

        void StopProgress()
        {
            if (_thread != null && _thread.IsAlive)
            {
                //_thread.Abort();
                _thread.Join(3000);
            }
        }

        void Progress(Object state)
        {
            Int32 left = Console.CursorLeft;

            // 设置光标不可见
            Boolean cursorVisible = Console.CursorVisible;
            Console.CursorVisible = false;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                try
                {
                    Int32 i = Index;
                    if (i >= Times) break;

                    if (i > 0 && sw.Elapsed.TotalMilliseconds > 10)
                    {
                        Double d = (Double)i / Times;
                        Console.Write("{0,7:n0}ms {1:p}", sw.Elapsed.TotalMilliseconds, d);
                        Console.CursorLeft = left;
                    }
                }
                //catch (ThreadAbortException) { break; }
                catch { break; }

                Thread.Sleep(500);
            }
            sw.Stop();

            Console.CursorLeft = left;
            Console.CursorVisible = cursorVisible;
        }
        #endregion

        #region> PInvoke
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool QueryThreadCycleTime(IntPtr threadHandle, ref ulong cycleTime);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetThreadTimes(IntPtr hThread, out long lpCreationTime, out long lpExitTime, out long lpKernelTime, out long lpUserTime);

        private static ulong GetCycleCount()
        {
            ulong cycleCount = 0;
            QueryThreadCycleTime(GetCurrentThread(), ref cycleCount);
            return cycleCount;
        }

        private static long GetCurrentThreadTimes()
        {
            long l;
            long kernelTime, userTimer;
            GetThreadTimes(GetCurrentThread(), out l, out l, out kernelTime, out userTimer);
            return kernelTime + userTimer;
        }
        #endregion

        #region> 重载ToString()
        /// <summary>
        /// 已重载。输出依次分别是：执行时间、CPU线程时间、时钟周期、GC代数
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("\tExcute Time:\t{0,7:n0}ms\r\n \tThread Time:\t{1,7:n0}ms\r\n \tCpuCycles:\t{2,17:n0}\r\n \tGC[Gen]:\t{3,6}/{4}/{5}\r\n", Elapsed.TotalMilliseconds, ThreadTime / 10000, CpuCycles, Gen[0], Gen[1], Gen[2]);
        }
        #endregion
    }
}
