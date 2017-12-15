


namespace java com.javabloger.gen.code   #  注释1

struct HelloRequest {   #  注释2 
    1: string Name
  }

struct HelloReply {   #  注释3
    1: string Message
  }


service Helloword {  #  注释4
    HelloReply SayHello(1:HelloRequest request)  #  注释5
}