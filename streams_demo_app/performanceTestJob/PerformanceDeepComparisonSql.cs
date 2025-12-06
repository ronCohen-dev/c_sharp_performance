namespace StreamsDemoApp.performanceTestJob;

using Microsoft.EntityFrameworkCore;
using StreamsDemoApp.repository.sqlServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Cistern.ValueLinq;

public class PerformanceDeepComparisonSql
{
    private readonly SqlServerContext _sql;

    public PerformanceDeepComparisonSql()
    {
        _sql = new SqlServerContext();
    }

    public async Task RunAsync()
    {
        var sqlData = await _sql.Persons.ToListAsync();
        Console.WriteLine($" {sqlData.Count:N0} rows in sql \n");

        var results = new List<(string Name, long Time, int Count, string Explanation)>
        {
            Measure("For Loop", sqlData, FilterWithFor, "low-level iteration\""),
            Measure("Foreach", sqlData, FilterWithForeach, "enumerator overhead"),
            Measure("LINQ", sqlData, FilterWithLinq, "iterator layers"),
            Measure("Parallel LINQ", sqlData, FilterWithParallelLinq, "multi-core execution"),
            Measure("Span<T>", sqlData, FilterWithSpan, "memory efficient"),
            Measure("ValueLinq", sqlData, FilterWithValueLinq, "fusion optimization")
        };


        foreach (var (name, time, count, explanation) in results)
        {
            Console.Write($"{name,-20}");
            Console.ResetColor();
            Console.WriteLine($"{time,5} ms | condition found: {count,5}");
            Console.WriteLine($" ** {explanation}\n");
        }
    }

    private (string, long, int, string) Measure(string name, List<PersonRecord> data,
        Func<List<PersonRecord>, List<PersonRecord>> func, string explanation)
    {
        var sw = Stopwatch.StartNew();
        var result = func(data);
        sw.Stop();
        return (name, sw.ElapsedMilliseconds, result.Count, explanation);
    }

    /*
     *  regular for
     */
    private List<PersonRecord> FilterWithFor(List<PersonRecord> data)
    {
        var result = new List<PersonRecord>();
        for (int i = 0; i < data.Count; i++)
        {
            var p = data[i];
            if (p.Age > 25 &&
                p.Age < 60 &&
                p.Salary > 10000 &&
                p.Salary < 25000 &&
                p.City.StartsWith("A") &&
                p.Country.Length > 4 &&
                p.Email.Contains(".com") &&
                p.FirstName.Length > 3 &&
                p.LastName.Length > 3 &&
                p.JobTitle.Contains("Manager") &&
                p.CreatedAt.Year > 2023 &&
                p.JobTitle.Length < 30 &&
                !p.Email.Contains("test") &&
                p.City != p.Country &&
                p.FirstName[0] != 'Z')
            {
                result.Add(p);
            }
        }

        return result;
    }

    /*
     *  regular for each
     */
    private List<PersonRecord> FilterWithForeach(List<PersonRecord> data)
    {
        var result = new List<PersonRecord>();
        foreach (var p in data)
        {
            if (p.Age > 25 &&
                p.Age < 60 &&
                p.Salary > 10000 &&
                p.Salary < 25000 &&
                p.City.StartsWith("A") &&
                p.Country.Length > 4 &&
                p.Email.Contains(".com") &&
                p.FirstName.Length > 3 &&
                p.LastName.Length > 3 &&
                p.JobTitle.Contains("Manager") &&
                p.CreatedAt.Year > 2023 &&
                p.JobTitle.Length < 30 &&
                !p.Email.Contains("test") &&
                p.City != p.Country &&
                p.FirstName[0] != 'Z')
            {
                result.Add(p);
            }
        }

        return result;
    }

    /*
     *  linq
     */
    private List<PersonRecord> FilterWithLinq(List<PersonRecord> data)
    {
        return data
            .Where(p => p.Age > 25 &&
                        p.Age < 60 &&
                        p.Salary > 10000 &&
                        p.Salary < 25000 &&
                        p.City.StartsWith("A") &&
                        p.Country.Length > 4 &&
                        p.Email.Contains(".com") &&
                        p.FirstName.Length > 3 &&
                        p.LastName.Length > 3 &&
                        p.JobTitle.Contains("Manager") &&
                        p.CreatedAt.Year > 2023 &&
                        p.JobTitle.Length < 30 &&
                        !p.Email.Contains("test") &&
                        p.City != p.Country &&
                        p.FirstName[0] != 'Z')
            .ToList();
    }

    /*
     *  parallel linq
     * bottleneck כאן הוא לא גישה לזיכרון אלא חישוב התנאים עצמם
     * (for / foreach / span / ValueLinq) הם חד ליבתים ולכן יעבדו לאט יותר בחישובים מורכבים וריבויי חישובים
     * in this case when i have a lot data in the heap we should use parallel linq מטעם חלוקת ליבות ( CPU )
     *  ** if the heap not work on more then 130MB data should not use this case else span
     */
    private List<PersonRecord> FilterWithParallelLinq(List<PersonRecord> data)
    {
        return data
            .AsParallel()
            .Where(p => p.Age > 25 &&
                        p.Age < 60 &&
                        p.Salary > 10000 &&
                        p.Salary < 25000 &&
                        p.City.StartsWith("A") &&
                        p.Country.Length > 4 &&
                        p.Email.Contains(".com") &&
                        p.FirstName.Length > 3 &&
                        p.LastName.Length > 3 &&
                        p.JobTitle.Contains("Manager") &&
                        p.CreatedAt.Year > 2023 &&
                        p.JobTitle.Length < 30 &&
                        !p.Email.Contains("test") &&
                        p.City != p.Country &&
                        p.FirstName[0] != 'Z')
            .ToList();
    }

    /*
     *  span<T> like implementation as c++ value type
     * הצגת המקרה שעדיף להמנע ואיך לעבוד בצורה מקסימלית כדי לשפר את הביצועים ולעבוד כמו C++
     * המתרה היא להוריד מה garbage collector לעבוד מול מערך גדול ב heap שמחזיק במקומות שונים מצביעים ולא בצורה רציפה
     * ReadOnlySpan<PersonRecord> span = CollectionsMarshal.AsSpan(data);
     *  ** if the heap not work on more then 130MB data should use this case
     * (span pointer) -> [ref][ref][ref][ref][ref]...
     *(PersonRecord instance in heap)
     * this case we work with parallel linq for better performance because the data in heap is more then 130MB and
     * intensive cases ( if && if else ) and this a lot of work fot single cpu
     * כשכמות המידע ב heap קטן יחסית אז עדיף להשתמש בזה ואז מאחורי הקלעים הוא עובד בצורה רציפה על הזיכרון וכל אחד במערך מצביעה למקום הבאה בזיכרון בלי לקפוץ
     * בין מקומות בזיכרון מה שגורם לשיפור משמעותי בביצועים ובנוסף הוא לא מעמיס על ה heap and gc
     * למעשה שאתה פונה ישירות לזיכרון זה עובד מהר יותק לאומת עבודה מול כמות גדולה של data ואז אתה עובד מול אובייקטים ולא בצורה
     * ישירה לזיכרון רציף ואז הביצועים פחות טובים כמו שציינתי למעלה.
     */

    private List<PersonRecord> FilterWithSpan(List<PersonRecord> data)
    {
        ReadOnlySpan<PersonRecord> span = CollectionsMarshal.AsSpan(data);
        var result = new List<PersonRecord>();

        foreach (ref readonly var p in span)
        {
            if (p.Age > 25 &&
                p.Age < 60 &&
                p.Salary > 10000 &&
                p.Salary < 25000 &&
                p.City.StartsWith("A") &&
                p.Country.Length > 4 &&
                p.Email.Contains(".com") &&
                p.FirstName.Length > 3 &&
                p.LastName.Length > 3 &&
                p.JobTitle.Contains("Manager") &&
                p.CreatedAt.Year > 2023 &&
                p.JobTitle.Length < 30 &&
                !p.Email.Contains("test") &&
                p.City != p.Country &&
                p.FirstName[0] != 'Z')
            {
                result.Add(p);
            }
        }

        return result;
    }

    /*
     *  valueLinq
     */
    private List<PersonRecord> FilterWithValueLinq(List<PersonRecord> data)
    {
        return data
            .Where(p => p.Age > 25 &&
                        p.Age < 60 &&
                        p.Salary > 10000 &&
                        p.Salary < 25000 &&
                        p.City.StartsWith("A") &&
                        p.Country.Length > 4 &&
                        p.Email.Contains(".com") &&
                        p.FirstName.Length > 3 &&
                        p.LastName.Length > 3 &&
                        p.JobTitle.Contains("Manager") &&
                        p.CreatedAt.Year > 2023 &&
                        p.JobTitle.Length < 30 &&
                        !p.Email.Contains("test") &&
                        p.City != p.Country &&
                        p.FirstName[0] != 'Z')
            .ToList();
    }
}