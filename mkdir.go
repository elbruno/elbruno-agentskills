package main
import ("os"; "fmt")
func main() {
	os.MkdirAll("samples/AgentWithSkills", 0755)
	fmt.Println("Created")
}
