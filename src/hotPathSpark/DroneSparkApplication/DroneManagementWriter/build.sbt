name := "DroneManagementWriter"

version := "0.1"

scalaVersion := "2.11.12"
retrieveManaged := true
//comaddSbtPlugin("com.eed3si9n" % "sbt-assembly" % "0.14.6")
//addSbtPlugin("com.eed3si9n" % "sbt-assembly" % "0.11.2")
libraryDependencies += "com.typesafe.play" %% "play-ahc-ws-standalone" % "2.0.0-M1"
libraryDependencies += "com.typesafe.play" %% "play-ws-standalone-json" % "2.0.0-M1"
libraryDependencies += "org.json4s" %% "json4s-native" % "3.6.0-M4"
libraryDependencies += "joda-time" % "joda-time" % "2.10"
// https://mvnrepository.com/artifact/org.apache.httpcomponents/httpclient
libraryDependencies += "org.apache.httpcomponents" % "httpclient" % "4.5.6"
// https://mvnrepository.com/artifact/org.apache.httpcomponents/httpasyncclient
libraryDependencies += "org.apache.httpcomponents" % "httpasyncclient" % "4.1.4"

mergeStrategy in assembly := {

  case n if n.startsWith("reference.conf") => MergeStrategy.concat
  case x =>
    val oldStrategy = (assemblyMergeStrategy in assembly).value
    oldStrategy(x)

 // case _ => MergeStrategy.deduplicate

}


/*
assemblyMergeStrategy in assembly := {
  case PathList("reference.conf") => MergeStrategy.concat
}*/
