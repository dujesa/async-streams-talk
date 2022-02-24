#region Imports
using System.Data;
using System.Runtime.CompilerServices;
using System.Text.Json;
#endregion

    // 1. Enumerator basics
    // a) synchronous enumerables
    //YieldReturnDemo();
    // b) asynchronous enumerables
    //await AwaitAndYieldReturnDemo();

    // 2. Async enumerables: Real world use-case
    //await PagingApiDemo();

    // 2. System.Linq.Async 
    // 2.1. Lambde
    // a) Basics
    await BasicLinqDemo();
    // b) Async lambda
    //await AsyncLambdaLinqDemo();

    // 2.2. Terminalne metode - vracaju value
    // a) Basic terminal metoda
    //await TerminalLinqMethodDemo();
    // b) Terminal metoda sa async lambdom
    //await TerminalLinqMethodWithAsyncLambdaDemo();

    // 3. Steroidi za obican LINQ
    //await LinqOnSteroidsDemo();

    // 4. Cancelling Async streams 
    //await BasicCancellationDemo();
    //await ComplexCancellationDemo();



#region CancellingAsyncStreams


static async Task BasicCancellationDemo()
{
    // Timeoutam nakon 4 sekunde
    using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
    Console.WriteLine($"[{DateTime.Now:hh:mm:ss}] Bootam cancellation demo");
    await foreach (int item in CancellableSlowRange(cts.Token))
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss}] Item: {item}");
    }
}

static async Task ComplexCancellationDemo()
{
    Console.WriteLine($"[{DateTime.Now:hh:mm:ss}] Bootam kompleksni cancellation demo");
    await ConsumeStreamWithTimeout(CancellableSlowRange());
}

// Puno genericnija verzija od ove bazicne jer smo sigurni da primamo async enumerable
// takoder benefit je taj sto u pozivu povise ne moramo imati u callu prosljeden cancellation token
// jer ga sa WithCancellation callom uz pomoc kompajlera dobijemo u cts.Token zapravo tocno onaj token koji je 
// dobiven u argumentu u metodi CancellableSlowRange() i oznacen je sa atributom
static async Task ConsumeStreamWithTimeout(IAsyncEnumerable<int> items)
{
    using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
    await foreach (int item in items.WithCancellation(cts.Token))
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss}] Item: {item}");
    }
}

static async IAsyncEnumerable<int> CancellableSlowRange([EnumeratorCancellation] CancellationToken token = default)
{
    for (var i = 0; i < 10; i++)
    {
        await Task.Delay(i * TimeSpan.FromSeconds(0.1), token);
        yield return i;
    }
}
#endregion

#region BoostingLinq
static async Task LinqOnSteroidsDemo()
{
    // Slucaj u kojem imamo obican enumerable
    IEnumerable<int> query = Enumerable.Range(0, 100);

    // Zelja nam je da odradimo query sa async lambdom u Whereu
    IAsyncEnumerable<int> asyncQuery = query.ToAsyncEnumerable()
        .WhereAwait(async item =>
        {
            await Task.Delay(TimeSpan.FromSeconds(0.1));
            return item % 2 == 0;
        });

    Console.WriteLine($"[{DateTime.Now:hh:mm:ss}] Startam loopanje...");
    await foreach (var item in asyncQuery)
    {
        // Obratimo pozornost na to da su async enumerablesi one-item-at-time i nemaju nikakve veze sa konkurentnim callovima kao sta bi tipa Task.WhenAll, samo neblokirajucim
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss}] Item: {item}");
    }
}
#endregion

#region TerminalMethods
static async Task TerminalLinqMethodDemo()
{
    Console.WriteLine($"Startam terminalnu metodu: {DateTime.Now:hh:mm:ss}");
    int result = await SlowRange().CountAsync(item => item % 2 == 0);
    Console.WriteLine($"Kraj metode: {DateTime.Now:hh:mm:ss}");
}

static async Task TerminalLinqMethodWithAsyncLambdaDemo()
{
    Console.WriteLine($"Startam terminalnu metodu: {DateTime.Now:hh:mm:ss}");
    int result = await SlowRange().CountAwaitAsync(async item =>
    {
        //Dummy skupa operacija
        await Task.Delay(TimeSpan.FromSeconds(0.1));
        return item % 2 == 0;
    });
    Console.WriteLine($"Kraj metode: {DateTime.Now:hh:mm:ss}");
}
#endregion

#region AsyncLambda
static async Task AsyncLambdaLinqDemo()
{
    Console.WriteLine($"Bootam... {DateTime.Now:hh:mm:ss}");
    IAsyncEnumerable<int> query = SlowRange().WhereAwait(async item =>
    {
        // Nista bitno samo sam svaki query usporia za 0.1 kao dummy demo nekog asinkronog calla
        // Benefit: Da imamo tu dummy demo operaciju u basic primjeru bila bi bloirajuca, ode nije
        await Task.Delay(TimeSpan.FromSeconds(0.1));
        return item % 2 == 0;
    });

    await foreach (int item in query)
    {
        Console.WriteLine($"{DateTime.Now:hh:mm:ss} - Broj: {item}");
    }
}
#endregion

#region LINQ
static async Task BasicLinqDemo()
{
    Console.WriteLine($"{DateTime.Now:hh:mm:ss} Starting...");
    IAsyncEnumerable<int> query = SlowRange().Where(item => item % 2 == 0);
    await foreach (int item in query)
    {
        Console.WriteLine($"{DateTime.Now:hh:mm:ss} fetchan: {item}");
    }
}

static async IAsyncEnumerable<int> SlowRange()
{
    for (int i = 0; i <= 10; ++i)
    {
        await Task.Delay(i * TimeSpan.FromSeconds(0.1));
        yield return i;
    }
}
#endregion

#region AwaitAndYieldReturn
static async Task AwaitAndYieldReturnDemo()
{
    await foreach (int item in AwaitAndYieldReturn())
    {
        Console.WriteLine($"Broj: {item}");
    }

    ////To what it compiles?
    //await using (var enumerator = AwaitAndYieldReturn().GetAsyncEnumerator())
    //{
    //    while (await enumerator.MoveNextAsync())
    //    {
    //        var item = enumerator.Current;
    //        Console.WriteLine($"Broj: {item}");
    //    }
    //}
}

static async IAsyncEnumerable<int> AwaitAndYieldReturn()
{
    await Task.Delay(TimeSpan.FromSeconds(1));  // pause => await, enumerator pauses and returns incompleted task
    yield return 1;                                   // pause => produce value, enumerator produces value and can be called again
    await Task.Delay(TimeSpan.FromSeconds(1));  // pause => await
    yield return 2;                                   // pause => produce value
    await Task.Delay(TimeSpan.FromSeconds(1));  // pause => await
    yield return 3;                                   // pause => produce value
}
#endregion

#region YieldReturn
static void YieldReturnDemo()
{
    // As foreach pulls each item out => it calls block and at each yield return method stops, on second pull it resumes
    foreach (int item in YieldReturn())
    {
        Console.WriteLine($"Broj: {item}");
    }

    Console.WriteLine("-----");

    ////Trik pitanje sa whileom => deep dive to what it compiles
    //using (var enumerator = YieldReturn().GetEnumerator())
    //{
    //    // Enumerable => Enumerator
    //    while (enumerator.MoveNext())
    //    {
    //        var item = enumerator.Current;
    //        Console.WriteLine($"Broj: {item}");
    //    }
    //}
}

static IEnumerable<int> YieldReturn()
{
    // Deferred execution => state machine (show on sharplab example)
    yield return 1;
    yield return 2;
    yield return 3;
}
#endregion

#region PagingApi
static async Task PagingApiDemo()
{
    Console.WriteLine($"{DateTime.Now:hh:mm:ss} Booting demo...");
    await foreach (int item in PagingApi())
    {
        Console.WriteLine($"{DateTime.Now:hh:mm:ss} Got {item}");
    }
    Console.WriteLine("Paging api demo ended...");
}

static async IAsyncEnumerable<int> PagingApi()
{
    const int PageSize = 5;
    int offset = 0;

    while (true)
    {
        // Fetch page of results
        var jsonString = await DummyHttpClient.Get(PageSize, offset);

        // Produce pages for caller/consumer
        int[] results = JsonSerializer.Deserialize<int[]>(jsonString);
        foreach (int result in results)
            yield return result;

        // Logic for page navigation
        if (results.Length != PageSize)
            break;

        offset += PageSize;
    }
}


static class DummyHttpClient
{
    public static async Task<string> Get(int limit = 10, int offset = 0)
    {
        await Task.Delay(TimeSpan.FromSeconds(3));
        var result = Enumerable.Range(0, 13).Skip(offset).Take(limit).ToList();

        return JsonSerializer.Serialize(result);
    }
}
#endregion



