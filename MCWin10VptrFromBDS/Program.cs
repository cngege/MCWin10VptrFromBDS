using System;
using System.Globalization;
using Tools.Address;
using LevelDB;

// See https://aka.ms/new-console-template for more information
Console.Title = "你好 ~  搜索Minecraft Win10 版和BDS中类的虚表，以制作MCWin10Mod!";
Console.WriteLine("你好 ~");


const string BDS = "bedrock_server";
const string BDSMoudle = "bedrock_server.exe";
const string MCWin10 = "Minecraft.Windows";
const string MCWin10Moudle = "Minecraft.Windows.exe";
const int maxcount = 5000;

IntPtr MCWin10ClassVTAddress = IntPtr.Zero;
IntPtr BDSClassVTAddress = IntPtr.Zero;

IntPtr MCWin10MoudleBaseAddress = IntPtr.Zero;
IntPtr BDSMoudleBaseAddress = IntPtr.Zero;

int BDSPid = 0;
int MCWin10Pid = 0;

#if DEBUG
string? BDSSymPath = "C:\\Users\\CNGEGE\\Desktop\\MCBDS插件开发助手\\bedrock_server的PDB符号1.19.50.02.txt";
string? LevelDBPath = "C:\\Users\\CNGEGE\\Desktop\\SymDB";
#else
string? BDSSymPath = null;
string? LevelDBPath = null;
#endif



//提示输入
//0. 退出
//1. Print Info  //打印所有存储的信息
//2. 设置BDS符号文件位置
//3. 设置 MCWin10 中要查询虚表的类地址/虚表地址（比如Player类
//4. 设置 BDS     中要查询虚表的虚表地址（比如Player类
//5. 清除 MCWin10 中要查询虚表的类地址/虚表地址
//6. 清除 BDS     中要查询虚表的虚表地址
//7. 在MinecraftWin10版中查询方法地址在虚表中的位置
//8. 在BDS版中查询方法地址在虚表中的位置
//9. 同时根据类地址依次获取对应虚函数的地址
//10. 获取后写入文件


while (true)
{
    int? menu_select = Menu();
    if (menu_select == 0)
        break;
    if (menu_select == null)
        continue;
    //取PID
    MCWin10Pid = Address.GetPid(MCWin10);
    BDSPid = Address.GetPid(BDS);
    //取模块基地址
    if (MCWin10Pid != 0)
        MCWin10MoudleBaseAddress = Address.GetModuleAddr(MCWin10Pid,MCWin10Moudle);
    if (BDSPid != 0)
        BDSMoudleBaseAddress = Address.GetModuleAddr(BDSPid, BDSMoudle);
    if (menu_select == 1)
    {
        Console.WriteLine("const BDS = {0}", BDS);
        Console.WriteLine("const BDSMoudle = {0}", BDSMoudle);
        Console.WriteLine("const MCWin10 = {0}", MCWin10);
        Console.WriteLine("const MCWin10Moudle = {0}", MCWin10Moudle);
        Console.WriteLine("const maxcount = {0}", maxcount);
        Console.WriteLine("MCWin10ClassVTAddress = {0}", MCWin10ClassVTAddress.ToString("X16"));
        Console.WriteLine("BDSClassVTAddress = {0}", BDSClassVTAddress.ToString("X16"));
        Console.WriteLine("MCWin10MoudleBaseAddress = {0}", MCWin10MoudleBaseAddress.ToString("X16"));
        Console.WriteLine("BDSMoudleBaseAddress = {0}", BDSMoudleBaseAddress.ToString("X16"));

        Console.WriteLine("BDSPid = {0}", BDSPid);
        Console.WriteLine("MCWin10Pid = {0}", MCWin10Pid);
        Console.WriteLine("BDSSymPath = {0}", BDSSymPath);
        Console.WriteLine("LevelDBPath = {0}", LevelDBPath);

    }
    if (menu_select == 2)
    {
        if (BDSSymPath != null)
        {
            Console.WriteLine("old BDSSymPath = {0}", BDSSymPath);
        }
        Console.Write("请输入新的 BDSSymPath 文件位置:");
        var Path = Console.ReadLine();
        if (File.Exists(Path))
        {
            BDSSymPath = Path;
            //Console.WriteLine("成功");
            if(LevelDBPath != null)
            {
                string[] bdsym = Array.Empty<string>();
                Console.WriteLine("开始生成数据库 => {0}", LevelDBPath);
                //var db = DB.Open(LevelDBPath, new Options { CreateIfMissing = true });
                var db = new DB(new Options { CreateIfMissing = true }, LevelDBPath);
                Console.WriteLine("读取&分割符号文件中……");
                bdsym = File.ReadAllLines(BDSSymPath);

                var WOptions = new WriteOptions { Sync = false };
                var errorcount = 0;
                Dictionary<string, string> syms = new();

                for (int i=0; i < bdsym.Length; i++)
                {
                    if (bdsym[i].StartsWith("0x"))
                    {
                        var key = bdsym[i][..10];
                        var WriteVal = key + bdsym[i - 1].Remove(0, 7) + "\n";
                        try
                        {
                            Console.WriteLine("正在处理第 {0} 行", i.ToString());
                            //读取key
                            //优化 将有多处符号的地址存在内存中 最后统一写入
                            //var sucess = db.TryGet(ReadOptions.Default, key, out Slice val);
                            string val = db.Get(key);
                            if (val != null)
                            {
                                //WriteVal = val.ToString() + WriteVal;
                                //db.Put(WOptions, key, WriteVal);
                                if (syms.ContainsKey(key))
                                {
                                    syms[key] = syms.GetValueOrDefault(key) + WriteVal;
                                }
                                else
                                {
                                    syms.Add(key, val.ToString() + WriteVal);
                                }
                            }
                            else
                            {
                                db.Put(key, WriteVal, WOptions);
                                //db.Put(WOptions, key, WriteVal);
                            }
                        }
                        catch
                        {
                            errorcount++;
                        }

                    }
                }
                bdsym.Clone();
                Console.WriteLine("第一轮写入结束,现在处理冗余符号");
                foreach(KeyValuePair<string,string> kv in syms){
                    try
                    {
                        db.Put(kv.Key, kv.Value, WOptions);
                        //db.Put(WOptions, kv.Key, kv.Value);
                    }
                    catch
                    {
                        errorcount++;
                    }
                    
                }

                Console.WriteLine("结束 此次处理共发生了 {0} 处错误",errorcount);
                db.Dispose();
                syms.Clear();
                GC.Collect();
            }
        }
        else
        {
            Console.WriteLine("文件不存在");
        }

    }
    if (menu_select == 3)
    {
        if (LevelDBPath != null)
        {
            Console.WriteLine("old LevelDBPath = {0}", LevelDBPath);
        }
        Console.WriteLine("请设置SymDB的数据库位置: ");
        var path = Console.ReadLine();
        if (path != null && path!= string.Empty)
        {
            LevelDBPath = path;
            Console.WriteLine("完成.");
        }
    }
    if (menu_select == 4)
    {
        Console.WriteLine("请设置 {0} 中你要查询虚表的类的地址，或虚表地址(16进制)", "MCWin10");
        var addr_str = Console.ReadLine();
        var oldaddr = MCWin10ClassVTAddress;
        if (addr_str?.Length >= 1 && addr_str[0] == '+')
        {
            if (addr_str.Length > 1)
            {
                //输入的地址以+开头 则表示该地址是 指向虚表的偏移
                addr_str = addr_str.Replace("+", null);
                var offset = StringToIntOrZeroHex(addr_str);
                MCWin10ClassVTAddress = (IntPtr)((long)MCWin10MoudleBaseAddress + offset);
                Console.WriteLine("{0}+0x{1} ==> 0x{2}", MCWin10Moudle, offset?.ToString("X8"), MCWin10ClassVTAddress.ToString("X16"));
            }
            else // =1
            {
                Console.WriteLine($"{addr_str} 非法");
            }
        }
        else
        {
            var addr = StringToIntPtrOrZero(addr_str);
            if (addr.ToInt64() > 0x7FF000000000)
            {
                MCWin10ClassVTAddress = addr;
            }
            else
            {
                //从类地址中读取虚表地址 后赋值
                MCWin10ClassVTAddress = Address.ReadValue_IntPtr64(addr, BDSPid, 0);
            }
        }

        //7FF6EFF4DE40
        Console.WriteLine("{0} [{1}] => [{2}]", "MCWin10ClassAddress", oldaddr.ToString("X16"), MCWin10ClassVTAddress.ToString("X16"));
        continue;
    }
    if (menu_select == 5)
    {
        Console.WriteLine("请设置 {0} 中你要查询虚表的类的地址，或虚表地址(16进制)", "BDS");
        var addr_str = Console.ReadLine();
        var oldaddr = BDSClassVTAddress;

        if (addr_str?.Length >= 1 && addr_str[0] == '+')
        {
            if(addr_str.Length > 1)
            {
                //输入的地址以+开头 则表示该地址是 指向虚表的偏移
                addr_str = addr_str.Replace("+", null);
                var offset = StringToIntOrZeroHex(addr_str);
                BDSClassVTAddress = (IntPtr)((long)BDSMoudleBaseAddress + offset);
                Console.WriteLine("{0}+0x{1} ==> 0x{2}", BDSMoudle, offset?.ToString("X8"), BDSClassVTAddress.ToString("X16"));
            }
            else // =1
            {
                Console.WriteLine($"{addr_str} 非法");
            }

        }
        else
        {
            var addr = StringToIntPtrOrZero(addr_str);
            if (addr.ToInt64() > 0x7FF000000000)
            {
                BDSClassVTAddress = addr;
            }
            else
            {
                //从类地址中读取虚表地址 后赋值
                BDSClassVTAddress = Address.ReadValue_IntPtr64(addr, BDSPid, 0);
            }
        }

        Console.WriteLine("{0} [{1}] => [{2}]", "BDSClassAddress", oldaddr.ToString("X16"), BDSClassVTAddress.ToString("X16"));
        continue;
    }
    if (menu_select == 6)
    {
        MCWin10ClassVTAddress = IntPtr.Zero;
    }
    if (menu_select == 7)
    {
        BDSClassVTAddress = IntPtr.Zero;
    }
    if (menu_select == 8)
    {
        if (MCWin10ClassVTAddress == IntPtr.Zero)
        {
            Console.WriteLine("{0} is Zero", "MCWin10ClassVTAddress");
            Console.WriteLine("请先为{0}设置好地址", "MCWin10ClassVTAddress");
            Console.ReadKey();
            continue;
        }
        Console.WriteLine("请输入一个地址(类地址:{0})> ", MCWin10ClassVTAddress.ToString("X16"));
        IntPtr queryAddr = StringToIntPtrOrZero(Console.ReadLine());
        var Vptr = MCWin10ClassVTAddress;
        //读取 MCWin10中的地址
        for (int i=0;i< maxcount; i++)
        {
            var callAddress = Address.ReadValue_IntPtr64(Vptr, MCWin10Pid, i*8);
            if (callAddress == queryAddr)
            {
                Console.WriteLine("在虚表的第 {0} 处找到了相符合的地址", i);
                break;
            }
            if (callAddress.ToInt64() % 16 != 0)
            {
                Console.WriteLine("虚表中共找到 {0} 个函数,但没找到目标地址", i);
                break;
            }
        }
    }
    if (menu_select == 9)
    {
        if (BDSClassVTAddress == IntPtr.Zero)
        {
            Console.WriteLine("{0} is Zero", "BDSClassVTAddress");
            Console.WriteLine("请先为{0}设置好地址", "BDSClassVTAddress");
            Console.ReadKey();
            continue;
        }
        Console.WriteLine("请输入一个地址(类地址:{0})> ", BDSClassVTAddress.ToString("X16"));
        IntPtr queryAddr = StringToIntPtrOrZero(Console.ReadLine());
        var Vptr = BDSClassVTAddress;
        for (int i = 0; i < maxcount; i++)
        {
            var callAddress = Address.ReadValue_IntPtr64(Vptr, BDSPid, i * 8);
            if (callAddress == queryAddr)
            {
                Console.WriteLine("在虚表的第 {0} 处找到了相符合的地址", i);
                break;
            }
            if (callAddress.ToInt64() % 16 != 0)
            {
                Console.WriteLine("虚表中共找到 {0} 个函数,但没找到目标地址", i);
                break;
            }
        }

    }
    if (menu_select == 10)
    {
        var MCWin10Vptr = IntPtr.Zero;
        if (MCWin10Pid != 0 && MCWin10ClassVTAddress != IntPtr.Zero)
        {
            MCWin10Vptr = MCWin10ClassVTAddress;
        }
        var BDSVptr = IntPtr.Zero;
        if (BDSPid != 0 && BDSClassVTAddress != IntPtr.Zero)
        {
            BDSVptr = BDSClassVTAddress;
        }

        if (MCWin10Vptr == IntPtr.Zero && BDSVptr == IntPtr.Zero)
        {
            Console.WriteLine("没有打开进程或没有设置类地址.退出");
            break;
        }

        

        for (int i = 0; i < maxcount; i++)
        {
            var MCWin10callAddress = IntPtr.Zero;
            var BDScallAddress = IntPtr.Zero;

            Console.Write("{0}. ", i);
            if (MCWin10Vptr != IntPtr.Zero)
            {
                MCWin10callAddress = Address.ReadValue_IntPtr64(MCWin10Vptr, MCWin10Pid, i * 8);
                Console.Write("{0} 虚函数: {1},偏移: {2} .  ", "MCWin10", MCWin10callAddress.ToString("X16"), MCWin10Moudle + "+" + (MCWin10callAddress.ToInt64() - MCWin10MoudleBaseAddress.ToInt64()).ToString("X8"));
            }
            if (BDSVptr != IntPtr.Zero)
            {
                BDScallAddress = Address.ReadValue_IntPtr64(BDSVptr, BDSPid, i * 8);
                Console.Write("{0} 虚函数: {1},偏移: {2} .  ", "BDS", BDScallAddress.ToString("X16"), BDSMoudle + "+" + (BDScallAddress.ToInt64() - BDSMoudleBaseAddress.ToInt64()).ToString("X8"));
            }
            Console.Write("\n");

            if (MCWin10callAddress.ToInt64() % 16 != 0 || BDScallAddress.ToInt64() % 16 != 0)
            {
                Console.WriteLine("虚表中共找到 {0} 个函数",i);
                break;
            }



            if (i >0 && i%10==0)
            {
                Console.Write("输入0跳回主菜单:");
                if (Console.ReadLine() == "0")
                {
                    break;
                }
            }
        }
        Console.Write("轮询结束.");
        Console.ReadLine();
    }

    if (menu_select == 11)
    {
        string filetext = "";
        //bool hasbdsym = false;
        //string[] bdsymdb = Array.Empty<string>();
        

        var MCWin10Vptr = IntPtr.Zero;
        if (MCWin10Pid != 0 && MCWin10ClassVTAddress != IntPtr.Zero)
        {
            MCWin10Vptr = MCWin10ClassVTAddress;
        }
        var BDSVptr = IntPtr.Zero;
        if (BDSPid != 0 && BDSClassVTAddress != IntPtr.Zero)
        {
            BDSVptr = BDSClassVTAddress;
        }

        if (MCWin10Vptr == IntPtr.Zero && BDSVptr == IntPtr.Zero)
        {
            Console.WriteLine("没有打开进程或没有设置类地址.退出");
            break;
        }

        //读取符号文件
        //if (BDSVptr != IntPtr.Zero/* && File.Exists(BDSSymPath)*/)
        //{
        //    Console.WriteLine("读取&分割符号文件中……");
        //    bdsym = File.ReadAllLines(BDSSymPath);
        //    hasbdsym = true;
        //}
        //var bdsymdb = DB.Open(LevelDBPath, Options.Default);
        var bdsymdb = new DB(new Options { }, LevelDBPath);
        for (int i = 0; i < maxcount; i++)
        {
            var MCWin10callAddress = IntPtr.Zero;
            var BDScallAddress = IntPtr.Zero;

#if DEBUG
            if (i >= 100)
            {
                //break;
            }
#endif

            Console.WriteLine("正在分析第{0}个虚表函数.", i);

            filetext += String.Format("虚表中第 {0} 个函数. \n", i);

            if (MCWin10Vptr != IntPtr.Zero)
            {
                MCWin10callAddress = Address.ReadValue_IntPtr64(MCWin10Vptr, MCWin10Pid, i * 8);
                filetext += String.Format("{0} 虚函数: {1},偏移: {2}\n", "MCWin10", MCWin10callAddress.ToString("X16"), MCWin10Moudle + "+" + (MCWin10callAddress.ToInt64() - MCWin10MoudleBaseAddress.ToInt64()).ToString("X8"));
            }
            if (BDSVptr != IntPtr.Zero)
            {
                BDScallAddress = Address.ReadValue_IntPtr64(BDSVptr, BDSPid, i * 8);
                string offsize = (BDScallAddress.ToInt64() - BDSMoudleBaseAddress.ToInt64()).ToString("X8");
                filetext += String.Format("{0} 虚函数: {1},偏移: {2}\n", "BDS", BDScallAddress.ToString("X16"), BDSMoudle + "+" + offsize);

                //if (bdsymdb.TryGet(ReadOptions.Default,"0x"+ offsize, out Slice val))
                string val = bdsymdb.Get("0x" + offsize);
                if (val != null)
                {
                    //for (int ii = 0; ii < bdsym.Length; ii++)
                    //{
                    //    if (bdsym[ii].StartsWith("0x" + offsize))
                    //    {
                    //        filetext += bdsym[ii - 1] + "\n";
                    //        filetext += bdsym[ii] + "\n";
                    //    }
                    //}

                    filetext += val.ToString();

                    if (filetext.Length > 10000)
                    {
                        File.AppendAllText(String.Format("C:/Users/{0}/Desktop/symVptr.txt", Environment.GetEnvironmentVariable("username")), filetext);
                        filetext = "";
                    }
                }

            }
            filetext += String.Format("\n");
            if (MCWin10ClassVTAddress == IntPtr.Zero && BDSClassVTAddress == IntPtr.Zero)
            {
                break;
            }
            if(MCWin10ClassVTAddress != IntPtr.Zero)
            {
                if(MCWin10callAddress.ToInt64() % 16 != 0)
                    break;
                if (MCWin10callAddress.ToInt64() - MCWin10MoudleBaseAddress.ToInt64() > 0xF0000000)
                    break;
            }

            if (BDSClassVTAddress != IntPtr.Zero)
            {
                if(BDScallAddress.ToInt64() % 16 != 0)
                    break;
                if(BDScallAddress.ToInt64() - BDSMoudleBaseAddress.ToInt64() > 0xF0000000)
                    break;
            }
        }
        bdsymdb.Dispose();
        File.AppendAllText(String.Format("C:/Users/{0}/Desktop/symVptr.txt", Environment.GetEnvironmentVariable("username")), filetext);
        Console.WriteLine("文件保存完成：{0}",String.Format("C:/Users/{0}/Desktop/symVptr.txt", Environment.GetEnvironmentVariable("username")));

    }
    if (menu_select == 12)
    {
        Console.WriteLine("DEBUG ");

        var ddb = new DB(new Options { CreateIfMissing = true }, "C:\\Users\\CNGEGE\\Desktop\\ddb");
        ddb.Put("li", "si");
        string v = ddb.Get("zhang");
        Console.WriteLine($"{v}");
    }

}
Console.Write("回车关闭程序.");
Console.ReadLine();



int? Menu()
{
    ConsoleColor consoleColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Menu");
    Console.WriteLine("0. 退出");
    Console.WriteLine("1. Print Info");
    Console.WriteLine("2. 选择BDS符号文件位置，并生成SymDB数据库,请先设置SymDB数据库位置");
    Console.WriteLine("3. 选择SymDB数据库位置(不包含\\)");
    Console.WriteLine("4. 设置 MCWin10 中要查询虚表的类地址/虚表地址（比如Player类");
    Console.WriteLine("5. 设置 BDS     中要查询虚表的虚表地址（比如Player类");
    Console.WriteLine("6. 清除 MCWin10 中要查询虚表的类地址/虚表地址");
    Console.WriteLine("7. 清除 BDS     中要查询虚表的虚表地址");
    Console.WriteLine("8. 在Win10版中查询方法地址在虚表中的位置");
    Console.WriteLine("9. 在BDS版中查询方法地址在虚表中的位置");
    Console.WriteLine("10. 根据类地址依次获取对应虚函数的地址");
    Console.WriteLine("11. 根据类地址依次获取对应虚函数的地址保存在桌面文件中");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("输入一个序号选择你要使用的功能 > ");
    Console.ForegroundColor = ConsoleColor.Green;

    Console.ForegroundColor = consoleColor;
    var ret = StringToIntOrZero(Console.ReadLine());
    if (ret > 12)
    {
        ret = 0;
    }
    return ret;
}

int? StringToIntOrZero(string? num)
{
    if (num == string.Empty || num == null)
    {
        return null;
    }
    if (int.TryParse(num, out int retnum))
    {
        return retnum;
    }
    else
    {
        return 0;
    }
}

int? StringToIntOrZeroHex(string? num)
{
    if (num == string.Empty || num == null)
    {
        return null;
    }
    if (int.TryParse(num, NumberStyles.HexNumber, null, out int retnum))
    {
        return retnum;
    }
    else
    {
        return 0;
    }
}

Int64 StringToInt64OrZero(string? num)
{
    if (num != string.Empty && num != null && Int64.TryParse(num, NumberStyles.HexNumber,null, out Int64 retnum))
    {
        return retnum;
    }
    else
    {
        return 0;
    }
}

IntPtr StringToIntPtrOrZero(string? num)
{
    return new IntPtr(StringToInt64OrZero(num));
}