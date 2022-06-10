open Expecto
module Program =
    let [<EntryPoint>] main argv = 
        Tests.runTestsInAssembly defaultConfig argv
