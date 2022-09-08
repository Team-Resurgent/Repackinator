using RepackinatorUI;

try
{
    var application = new Application();
    application.Run();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
    Console.WriteLine("Press enter to close.");
    Console.ReadLine();
}




