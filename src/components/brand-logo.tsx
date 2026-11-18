import Image from "next/image";
import styles from "./brand-logo.module.css";

export function BrandLogo({ prominent = false }: { prominent?: boolean }) {
  return (
    <span className={`${styles.mark} ${prominent ? styles.prominent : ""}`}>
      <Image
        src="/brand/botzone-logo.png"
        alt="BotZone"
        width={1254}
        height={1254}
        sizes={prominent ? "210px" : "(max-width: 650px) 150px, 175px"}
        loading={prominent ? "lazy" : "eager"}
        className={styles.image}
      />
    </span>
  );
}
