import { useState } from "react";
import { useAuth } from "../context/AuthContext";
import { useNavigate } from "react-router-dom";
import axios from "axios";

const Auth = () => {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [fullName, setFullName] = useState("");
    const [isRegistering, setIsRegistering] = useState(false);
    const { login } = useAuth();
    const navigate = useNavigate();

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            if (isRegistering) {
                await axios.post("https://localhost:5000/api/auth/register", { fullName, email, password });
                alert("Registration successful! You can now log in.");
                setIsRegistering(false);
            } else {
                const response = await axios.post("https://localhost:5000/api/auth/login", { email, password });
                login(response.data.token);
                navigate("/admin");
            }
        } catch (error) {
            alert("An error occurred. Please try again.");
        }
    };

    return (
        <div>
            <h2>{isRegistering ? "Register" : "Login"}</h2>
            <form onSubmit={handleSubmit}>
                {isRegistering && (
                    <input type="text" placeholder="Full Name" value={fullName} onChange={(e) => setFullName(e.target.value)} required />
                )}
                <input type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)} required />
                <input type="password" placeholder="Password" value={password} onChange={(e) => setPassword(e.target.value)} required />
                <button type="submit">{isRegistering ? "Register" : "Login"}</button>
            </form>
            <button onClick={() => setIsRegistering(!isRegistering)}>
                {isRegistering ? "Already have an account? Login" : "Don't have an account? Register"}
            </button>
        </div>
    );
};

export default Auth;
