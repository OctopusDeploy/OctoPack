interface Person2 {
    firstname: string;
    lastname: string;
}

function greeter2(person: Person) {
    return "Hello, " + person.firstname + " " + person.lastname;
}

var user = { firstname: "Jane", lastname: "User" };
