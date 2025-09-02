You can filter certain parts of the trackmeta data, by using a single char follow by a colon, and the search term is terminated by a semicolon or the end of the string.

t => track name

c => charter name

s => subtitle text

a => artist name

b => bpm

so `s:hello; c:me` searches for charts that have hello in the subtitle text as well as me in the charter text 

and a blank category like searching for just `censored` will match either track or charter name.

bpm takes a slightly different format like `b:>=200` valid options are `'', '=', '<', '<=', '>', and '>='`
