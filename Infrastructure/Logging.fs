namespace FsServiceFramework

open System
open System.ComponentModel.DataAnnotations
open System.Runtime.CompilerServices
open System.Data.Entity

type LogMessage = 
    | Debug of string
    | Warning of string
    | Error of string

type FileLocation = {FileName:string; Location:int}

type LogEntry(memberName:string, message:LogMessage) =
    [<Key>] 
    member val LogId = Guid.NewGuid() with get, set
    member val When = DateTime.Now with get, set
    member val MemberName = memberName with get, set
    // member val Location = FileLocation with get, set
    member val Message = message with get, set

type LogDbModel() =
    inherit DbContext("name=LogDbModel")

    [<DefaultValue>]
    val mutable entries: DbSet<LogEntry>
    member public this.LogEntries
        with get() = this.entries
        and set v = this.entries <- v

type public Log() =
    static member public 
            Out(message:LogMessage, 
                [<CallerMemberName>] ?memberName: string,
                [<CallerFilePath>] ?path: string,
                [<CallerLineNumber>] ?line: int) =
        Console.WriteLine(message)
        use model = new LogDbModel()
        // model.Database.Create() |> ignore
        let newEntry = LogEntry("", message)
        let entries = model.LogEntries
        entries |> ignore
        // entries.Add(newEntry) |> ignore
        // model.SaveChanges() |> ignore

module Logging = ()
