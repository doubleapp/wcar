# IGNORE THIS SKILL IT IS NOT VALIDATED!
# IGNORE THIS SKILL IT IS NOT VALIDATED!
# IGNORE THIS SKILL IT IS NOT VALIDATED!

## Prompt approach, didn't work as there was no web access
I'd like you to scrape an html page of a swagger link I'll give you and read it to create a concise summary of how the api works for you to implement it in this project. Make sure you methodically expand all the UI options to get all the information from the web page. The concise file should include only information describing the api logic. No need for any descriptions unless they describe key attributes that will help implementing the api. You need to make sure to get all data types, data structure, keys and allowed values and value configuration, which params are requried and which are optional. Write down the output in ./docs/swagger/pulseem-direct-api.md

## General

If you need to read a swagger file (local or from the web) you need to acomplish the following general steps:
- Consume the file
- Install a util to convert it to an .md file
- Convert it to a concise .md file for context optimization

It is key that you want miss out on key detailed in the process to prevent future mistakes

**This applies on the process of updating a swagger file** 

## Steps

### Consume the file
- Ask for a link or for a path
- Get the .json version of the file 

### Install 
- Install widdershins npm package as a dev-dependency
- Read the util options and understand them (use --help)

### Conver the file with the util
- Build the command to convert the file
- Make sure to try and reduce any non-essential information from the file in this step. E.g. Select to keep code examples only for programming languages relevant for the project 
- Write the result in this folder ./docs/swagger/ (create it if it's missing)
- Name the file <api-name>.full.md

### Reduce content 
- Read the full.md file and write a new file in the same location called <api-name>.concise.md
- In this file remove anything that doesn't describe the api logic directly 
- Make sure to keep:
    - Parameter names
    - Whether a param is required or not
    - The data structure including keys and values 
    - Any key information about how the api works

### Cleanup
- Remove the widdershins package from the project
- Remove any temporary files that were created 

### Report
- Output a report explaining the steps completed 
- Include line counts for the two output files 
