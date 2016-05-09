﻿module ForneverMind.Posts

open System.IO

open Freya.Core
open Freya.Router

open ForneverMind.Models

let postFilePath =
    let htmlExtension = ".html"
    freya {
        let! language = Common.routeLanguage
        let! maybeName = Route.Atom_ "name" |> Freya.Lens.getPartial
        let fileName = maybeName |> Option.orElse ""
        let name = Path.GetFileNameWithoutExtension fileName
        let extension = Path.GetExtension fileName
        let filePath =
            if extension = htmlExtension
            then Path.Combine (Config.postsDirectory, language, name + ".md")
            else "not-found.md"
        if not <| Common.pathIsInsideDirectory Config.postsDirectory filePath then failwith "Invalid file name"
        return filePath
    } |> Freya.memo

let checkPostExists =
    freya {
        let! filePath = postFilePath
        return File.Exists filePath
    }

let lastModified =
    freya {
        let! filePath = postFilePath
        return Common.dateTimeToSeconds (File.GetLastWriteTimeUtc filePath)
    }

let allPosts language =
    let directory = Path.Combine (Config.postsDirectory, language)
    if not <| Common.pathIsInsideDirectory Config.postsDirectory directory then failwithf "Access error"
    Directory.GetFiles directory
    |> Seq.map (fun filePath ->
        use reader = new StreamReader (filePath)
        Markdown.processMetadata filePath reader)
    |> Seq.sortByDescending (fun x -> x.Date)
    |> Seq.toArray
