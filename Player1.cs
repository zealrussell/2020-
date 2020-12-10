using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BasicClass;
using Interface;

namespace PlayerController
{

    public class Player1 : SuperPlayer
    {
        //结构体
        public struct Pt
        {
            public int x;
            public int y;
            public Pt(int _x, int _y)
            {
                x = _x;
                y = _y;
            }
            public override string ToString()
            {
                return "x:" + x + " y:" + y;
            }

            public static float Len(Pt a, Pt b)
            {
                return (float)Math.Pow((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y), 0.5);
            }


        }


        const int CountryID = 1;

        Pt[] countrypt = new Pt[4] { new Pt(2, 2), new Pt(2, 39), new Pt(39, 2), new Pt(39, 39) };
        //定义各种矩阵
        int[,] map = new int[41, 41];
        float[,] willMap = new float[41, 41];
        float[,] threatMap = new float[41, 41];
        float[,] finalWillMap = new float[41, 41];
        int[,] moveThreatMap = new int[41, 41];
        float[,] powerMap = new float[41, 41];

        List<Ranker> myRanker = new List<Ranker>();
        List<Ranker> enemyRanker = new List<Ranker>();
        List<Pt> willList = new List<Pt>(4);   //获得前四个最大的收益点
        List<Ranker> sendList = new List<Ranker>();

        float LEFTIME;
        int mflag = 0, r1;

        //定义各种参数值 


        //----------------------------------------------------

        public override void Awake() //此方法仅在游戏开始时被调用一次，用于初始化
        {
            teamName = "我没问题队"; //设置队伍名称（必须)
            r1 = BuyRanker(3, false);
        }

        public override void Update() //此方法在游戏进行时每0.02s被调用一次
        {
            try
            {
                UpdateInfo();  //更新信息               
                foreach (var best in willList)   //遍历收益表
                {
                    float myPower = 0;
                    calPower(best);
                    if (myRanker.Count == 0) return;
                    if (willList.Count <= 0) return;
                    sendList.Clear();

                    foreach (var ranker in myRanker)                       //遍历没有任务的士兵
                    {
                        //Log("移动" + djstl(new Pt(10,10),ranker) );
                        do
                        {
                            sendList.Add(ranker);                          //将士兵加入待派送列表
                            myPower += getPower(ranker);                   //收益点的兵力减少 
                        } while (powerMap[best.x, best.y] > myPower);      //如果兵力还不够

                        if (powerMap[best.x, best.y] <= myPower)
                        {         // 如果兵力够了，就把士兵全部送到收益点
                            RunAI(sendList, best);
                            break;
                        }
                    }

                }
            }
            catch (Exception e)
            {
                //Log(e.ToString());
                return;
            }
            return;
        }

        //----------------------------MAIN------------------------            

        public void RunAI(List<Ranker> sendList, Pt best)
        {
            foreach (Ranker ranker in sendList)
            {
                myRanker.Remove(ranker);    //派出
                if (!TryMove(ranker, best)) // 如果到了目的点
                {
                    //Log("地图属性："+map[1,30]);
                    int act = getMap(new Pt(best.x, best.y));  // 由目标点的属性进行操作
                    if (act == 6) // 打最弱的人
                    {
                        int id = getWeakest(ranker);               //  id = 最弱的敌人    
                        AttackRanker(ranker.ID, id);
                    }
                    else if (act == 5)              //泉水
                    {
                        AttackSpring(ranker.ID);
                    }
                    else if (act <= 4 && act >= 1) // 基地
                    {
                        AttackCountry(ranker.ID, act);
                    }
                }
            }
        }

        Boolean TryMove(Ranker ranker, Pt best)       //判断是否还需要移动
        {
            Pt me = new Pt(ranker.x, ranker.y);
            if (getMap(best) != 0)                  //如果是空地，移动到目的点，否则移动到攻击范围内
                if (Pt.Len(me, best) <= ranker.range)
                    return false;

            //int move = djstl(best, ranker);//djstl(best,ranker);//getWay(ranker,best);   //下一步           
            //if (move != 5)
            //    MoveRanker(ranker.ID, move);
            //else return false;


            Pt move = best;
            if (move.x > me.x)
                MoveRanker(ranker.ID, 2);
            else if (move.x < me.x)
                MoveRanker(ranker.ID, 1);
            else if (move.y < me.y)
                MoveRanker(ranker.ID, 3);
            else if (move.y > me.y)
                MoveRanker(ranker.ID, 4);
            else return false;

            return true;     //移动成功
        }


        //------------------------定义各种SET、GET方法------------------------------
        bool MapAvailable(Pt pt)  //判断地图是否越界
        {
            if (pt.x < 1 || pt.x > 40 || pt.y < 1 || pt.y > 40)
                return false;
            return true;
        }
        int getMap(Pt pt) { return map[pt.x, pt.y]; }          //获取map属性     

        List<Ranker> getRankerList(Pt point)                          //获取地点的所有ranker列表  √
        {
            int x = point.x;
            int y = point.y;
            List<int> temp = GetStandList(x, y);
            List<Ranker> rankerList = new List<Ranker>();
            foreach (int i in temp)
                rankerList.Add(GetRankerInfo(i));
            return rankerList;
        }
        List<Ranker> getEnemyRankerList(Pt point)                          //获取地点的敌方ranker列表 √
        {
            int x = point.x;
            int y = point.y;
            List<int> temp = GetStandList(x, y);
            List<Ranker> rankerList = new List<Ranker>();
            foreach (int i in temp)
            {
                Ranker ranker = GetRankerInfo(i);
                if (ranker.countryID != CountryID)
                    rankerList.Add(ranker);
            }
            return rankerList;
        }


        //----------------------更新、计算----------------------------------------------------
        void UpdateInfo()
        {
            RefreshBuy();
            RefreshTime();
            ProtectBase();
            RefreshRanker();
            RefreshMap();
            SelfProtect();

            compute_threatMap();    //更新威胁     √     
            compute_willMap();      //更新收益     √
            compute_moveThreatMap();
        }

        void ProtectBase()
        {
            Pt my = countrypt[CountryID - 1];
            for (int i = 1; i < 41; i++)
            {
                for (int j = 1; j < 41; j++)
                {
                    Pt me = new Pt(i, j);
                    if (Pt.Len(me, my) <= 10)
                    {
                        if (threatMap[i, j] > 300) willMap[my.x, my.y] += 10000;
                    }
                }

            }
            willMap[my.x, my.y] += 1000;
        }
        void RefreshBuy()
        {
            Country myCountry = GetCountryInfo(CountryID);
            while (myCountry.money > 30)
            {
                if (myCountry.chance > 0)
                {
                    BuyRanker(3, true);
                    BuyRanker(1, true);
                }
                BuyRanker(3, false);
                BuyRanker(1, false);
            }

        }
        void RefreshTime()         //更新时间
        {
            LEFTIME = GetLeftTime();
        }
        void RefreshRanker()        //更新士兵信息 √
        {
            myRanker.Clear();
            enemyRanker.Clear();
            foreach (int i in new int[] { 1, 2, 3, 4 })
            {
                Country tmp = GetCountryInfo(i);
                if (tmp.alive)
                {
                    List<int> rankerList = GetRankerList(i);
                    Ranker r;
                    //获取该国信息                  
                    if (i == CountryID)
                    {      //如果本国,加入myCountry
                        foreach (var k in rankerList)
                        {
                            r = GetRankerInfo(k);
                            myRanker.Add(r);
                        }

                    }
                    else
                    {
                        foreach (var k in rankerList)
                        {
                            r = GetRankerInfo(k);
                            enemyRanker.Add(r);
                        }
                    }

                }

            }
        }
        void RefreshMap()            //更新地图   √
        {
            for (int i = 1; i < 41; i++)
            {
                for (int j = 1; j < 41; j++)
                {
                    map[i, j] = GetMapCubeType(i, j);
                    if (map[i, j] == 5)
                        if (!GetSpringInfo().alive) map[i, j] = 0;

                }
            }
            if (enemyRanker == null) return;
            foreach (var i in enemyRanker)
            {
                map[i.x, i.y] = 6; //敌方士兵
            }
        }


        const float SPRINGWILL = 10000;
        const float BASEWill = 1000;

        const float BASEW1 = 50;
        const float BASEW2 = 50;
        const float BASEW3 = 30;

        //--------------------计算向量  通过 威胁 找路径到达 收益 最大的点 -------------------      
        public void compute_threatMap()     //地图威胁 √
        {
            for (int i = 1; i < 41; i++)
                for (int j = 1; j < 41; j++)
                    threatMap[i, j] = 0;
            for (int i = 1; i <= 4; i++)
            {
                if (i == CountryID)
                    continue;
                foreach (int j in GetRankerList(i))
                {
                    Ranker r = GetRankerInfo(j);
                    int x = r.x, y = r.y;
                    float fenzi;
                    fenzi = (float)r.atk * (float)r.frequency * (float)r.health * (float)r.range * (float)r.speed;
                    for (int xx = x - 5; xx <= x + 5; xx++)
                    {
                        for (int yy = y - 5; yy <= y + 5; yy++)
                        {
                            if (xx < 1 || xx > 40 || yy < 1 || yy > 40)
                            {
                                continue;
                            }
                            float distance = (float)Math.Sqrt((x - xx) * (x - xx) + (y - yy) * (y - yy));
                            distance = distance + (float)1;
                            threatMap[xx, yy] = fenzi / distance;
                        }
                    }
                }
            }


        }

        //保家：计算家和threat，如果家的threat 大，立即加大 家的willness；      
        public void compute_willMap()           // 地图收益 √
        {
            Pt zero = new Pt(1, 1), me;
            var m1 = new Tuple<float, Pt>(0, zero);
            var m2 = new Tuple<float, Pt>(0, zero);
            var m3 = new Tuple<float, Pt>(0, zero);
            var m4 = new Tuple<float, Pt>(0, zero);

            for (int i = 1; i < 41; i++)        //清零
                for (int j = 1; j < 41; j++)
                    willMap[i, j] = 0;

            foreach (Ranker r in enemyRanker)       //士兵收益
            {
                int x = r.x, y = r.y;
                float fenzi;
                fenzi = (float)(r.health * r.bonus);  //收益 = 生命值 * 击杀收益 * 输出伤害
                for (int xx = x - 5; xx <= x + 5; xx++)
                {
                    for (int yy = y - 5; yy <= y + 5; yy++)
                    {
                        if (xx < 1 || xx > 40 || yy < 1 || yy > 40)
                            continue;
                        float distance = (float)Math.Sqrt((x - xx) * (x - xx) + (y - yy) * (y - yy));
                        distance = distance + (float)1;
                        willMap[xx, yy] += fenzi / distance;
                    }
                }
            }

            foreach (int i in new[] { 1, 2, 3, 4 })
            {
                setBaseWill(i);
            }

            for (int i = 1; i < 41; i++)
            {
                for (int j = 1; j < 41; j++)
                {
                    me = new Pt(i, j);
                    int act = getMap(me);
                    if (getMap(me) == 5) willMap[i, j] += SPRINGWILL;   //                    
                    //if (act >= 1 && act <= 4 && act != CountryID)
                    //{
                    //    if(GetCountryInfo(act).alive)
                    //        willMap[i, j] += 
                    //}

                    float willness = willMap[i, j];

                    var temp = new Tuple<float, Pt>(willness, me);
                    if (willness > m1.Item1)
                        m1 = temp;
                    else if (willness > m2.Item1)
                        m2 = temp;
                    else if (willness > m3.Item1)
                        m3 = temp;
                    else if (willness > m4.Item1)
                        m4 = temp;
                }
            }
            //放进队列
            willList.Clear();
            willList.Add(m1.Item2);
            willList.Add(m2.Item2);
            willList.Add(m3.Item2);
            willList.Add(m4.Item2);

        }
        public void compute_moveThreatMap()
        {
            for (int i = 1; i < 41; i++)        //清零
                for (int j = 1; j < 41; j++)
                    moveThreatMap[i, j] = (int)threatMap[i, j] / 1000;

        }

        public void calPower(Pt point)          //计算目标点兵力 √
        {
            for (int i = 1; i < 41; i++)
                for (int j = 1; j < 41; j++)
                    powerMap[i, j] = 0;

            List<Ranker> rankerList = getRankerList(point);

            foreach (Ranker r in rankerList)
            {
                if (r.countryID != CountryID)
                {
                    float p = getPower(r);
                    powerMap[point.x, point.y] += p * p;
                }
            }

            // Log("兵力"+powerMap[1,30]);
        }
        public float getPower(Ranker ranker)            //获取兵力 √
        {
            float p = ranker.atk * ranker.atkCd * ranker.health;
            return p;
        }

        public void setBaseWill(int id)
        {
            Country country = GetCountryInfo(id);
            float willness = BASEWill / (country.soldierNum + 1);
            if (id == 1)
            {
                willMap[2, 2] += willness;
            }
            else if (id == 2)
            {
                willMap[2, 39] += willness;
            }
            else if (id == 3)
            {
                willMap[39, 2] += willness;
            }
            else if (id == 4)
            {
                willMap[39, 39] += willness;
            }
        }
        public void finalWill()  //最终收益 11.26未完成
        {
            for (int i = 1; i < 41; i++)
                for (int j = 1; j < 41; j++)
                {
                    finalWillMap[i, j] = threatMap[i, j] + willMap[i, j];
                }
        }

        public void calSelfWill()
        {
            foreach (Ranker r in myRanker)       //士兵收益
            {
                int x = r.x, y = r.y;
                float fenzi;
                fenzi = (float)(r.health * r.atk) / 100;  //收益 = 生命值 * 击杀收益 * 输出伤害
                for (int xx = x - 5; xx <= x + 5; xx++)
                {
                    for (int yy = y - 5; yy <= y + 5; yy++)
                    {
                        if (xx < 1 || xx > 40 || yy < 1 || yy > 40)
                            continue;
                        float distance = (float)Math.Sqrt((x - xx) * (x - xx) + (y - yy) * (y - yy));
                        distance = distance + (float)1;
                        willMap[xx, yy] += fenzi / distance;
                    }
                }
            }
        }


        //-----------------------------------------------------------
        public void SelfProtect()
        {
            foreach (Ranker ranker in myRanker)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (i == CountryID - 1) continue;
                    Pt me = new Pt(ranker.x, ranker.y);
                    if (ranker.range >= Pt.Len(me, countrypt[i]))
                    {
                        AttackCountry(ranker.ID, i + 1);
                    }
                }
                int act = getWeakest(ranker);
                if (act != 0)
                {
                    AttackRanker(ranker.ID, act);
                }
            }

        }

        /// 返回ranker所有攻击范围内的敌人list
        public List<Ranker> canATK(Ranker ranker)
        {
            int range = ranker.range;
            Pt me = new Pt(ranker.x, ranker.y);
            Pt en;
            List<Ranker> enemy = new List<Ranker>();
            foreach (var r in enemyRanker)
            {
                en = new Pt(r.x, r.y);
                if (Pt.Len(me, en) <= range * range)
                    enemy.Add(r);
            }
            return enemy;
        }

        /// 返回r中血量最低的士兵的编号   
        public int weakestRanker(List<Ranker> rankerlist)
        {
            if (rankerlist.Count <= 0) return 0;
            int max = 1000;
            int id = 0;
            foreach (var ranker in rankerlist)
            {
                if (ranker.health < max)
                {
                    max = ranker.health;
                    id = ranker.ID;
                }
            }
            return id;
        }

        public int getWeakest(Ranker ranker)
        {
            return weakestRanker(canATK(ranker));
        }
        public int nearestEnemy(int ranker)
        {
            int near = 0;
            int dis = 1000;
            for (int i = 1; i <= 4; i++)
            {
                if (i == CountryID) continue;
                List<int> t = new List<int>();
                t = GetRankerList(i);
                foreach (int j in t)
                {
                    if (System.Math.Abs(GetRankerInfo(j).x - GetRankerInfo(ranker).x) + System.Math.Abs(GetRankerInfo(j).y - GetRankerInfo(ranker).y) < dis)
                    {
                        dis = System.Math.Abs(GetRankerInfo(j).x - GetRankerInfo(ranker).x) + System.Math.Abs(GetRankerInfo(j).y - GetRankerInfo(ranker).y);
                        near = j;
                    }
                }
            }
            return near;
        }

        double dis(int sx, int sy, int ex, int ey)
        {

            return System.Math.Sqrt((sx - ex) * (sx - ex) + (sy - ey) * (sy - ey));
        }





    }
}