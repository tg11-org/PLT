puts "Hello, World!"
set x 42
set y "test"
set z 3.14
set items [list 1 2 3 4 5]
if {[expr {$x > 40}]} {
    puts "x is greater than 40"
} else {
    puts "x is less than or equal to 40"
}
foreach item $items {
    puts $item
}
proc greet {name} {
    puts "Hello"
    puts $name
}
greet "Alice"
