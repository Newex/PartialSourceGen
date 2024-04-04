// See https://aka.ms/new-console-template for more information
using System;
using Sample.Models;

PartialPerson partialPerson = new()
{
    FirstName = "John",
    LastName = "Doe"
};

Console.WriteLine("Hello {0} {1} with ID: {2}", partialPerson.FirstName, partialPerson.LastName, partialPerson.ID);
partialPerson.CustomMethod();