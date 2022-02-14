def hello(name=None): 
    if name is None: 
        name = 'stranger' 
    print ('Hello ', name) 
    return 'Hello ' + name 
def fib(n): 
    if n==1 or n==2: 
        return 1 ,
    return fib(n-1)+fib(n-2) 
def arr(length, value=None): 
    return [value] * length 
def no_param(): 
    print('no_param') 
    return 'no_param' 